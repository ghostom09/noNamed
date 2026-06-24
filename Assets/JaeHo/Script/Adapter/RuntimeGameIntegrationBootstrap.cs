using BossSystem.Boss;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RuntimeGameIntegrationBootstrap
{
    private static GameObject _runner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        EnsureRunner();
        RoomClearMutationRewardSystem.ResetRewardHistory();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RuntimeGameIntegrationRunner.IntegrateScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureRunner();
        RuntimeGameIntegrationRunner.IntegrateScene();
    }

    private static void EnsureRunner()
    {
        if (_runner != null) return;

        _runner = new GameObject("[Runtime Game Integration]");
        Object.DontDestroyOnLoad(_runner);
        _runner.hideFlags = HideFlags.HideAndDontSave;
        _runner.AddComponent<RuntimeGameIntegrationRunner>();
    }
}

[DisallowMultipleComponent]
public sealed class MutationPickupRuntimeGuard : MonoBehaviour
{
    [SerializeField] private MutationPickup pickup;
    [SerializeField] private bool consumeAfterAppliedMutation = true;
    [SerializeField] private bool suppressPickupWhileMutationUiOpen = true;

    private static FieldInfo mutationPanelField;

    private bool armedByThisPickup;
    private bool disabledPickupForUi;
    private bool wasMutationUiOpen;
    private int stackTotalBeforeSelection;

    private void Awake()
    {
        BindReferences();
        mutationPanelField ??= typeof(MutationSelectUI)
            .GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private void OnEnable()
    {
        BindReferences();
    }

    private void Update()
    {
        bool uiOpen = IsMutationUiOpen();

        if (!uiOpen && IsPickupAcquirePressed())
        {
            ArmSelectionWatch();
        }

        if (suppressPickupWhileMutationUiOpen && uiOpen)
        {
            DisablePickupUpdate();
        }
        else if (disabledPickupForUi)
        {
            RestorePickupUpdate();
        }
    }

    private void LateUpdate()
    {
        bool uiOpen = IsMutationUiOpen();

        if (!armedByThisPickup && !wasMutationUiOpen && uiOpen && IsPickupAcquirePressed())
        {
            ArmSelectionWatch();
        }

        if (armedByThisPickup && wasMutationUiOpen && !uiOpen)
        {
            FinishSelectionWatch();
        }

        wasMutationUiOpen = uiOpen;
    }

    private void OnDisable()
    {
        RestorePickupUpdate();
        armedByThisPickup = false;
    }

    private void ArmSelectionWatch()
    {
        if (pickup == null || !pickup.IsPlayerInside)
        {
            return;
        }

        armedByThisPickup = true;
        stackTotalBeforeSelection = CountMutationStacks();
    }

    private void FinishSelectionWatch()
    {
        armedByThisPickup = false;

        if (!consumeAfterAppliedMutation)
        {
            RestorePickupUpdate();
            return;
        }

        if (CountMutationStacks() <= stackTotalBeforeSelection)
        {
            RestorePickupUpdate();
            return;
        }

        if (MutationDescriptionUI.Instance != null)
        {
            MutationDescriptionUI.Instance.Hide();
        }

        gameObject.SetActive(false);
    }

    private void DisablePickupUpdate()
    {
        if (pickup == null || disabledPickupForUi || !pickup.enabled)
        {
            return;
        }

        pickup.enabled = false;
        disabledPickupForUi = true;
    }

    private void RestorePickupUpdate()
    {
        if (pickup != null && disabledPickupForUi)
        {
            pickup.enabled = true;
        }

        disabledPickupForUi = false;
    }

    private void BindReferences()
    {
        if (pickup == null)
        {
            pickup = GetComponent<MutationPickup>();
        }
    }

    private static bool IsMutationUiOpen()
    {
        MutationSelectUI ui = MutationSelectUI.Instance;
        if (ui == null) return false;

        GameObject panel = mutationPanelField?.GetValue(ui) as GameObject;
        return panel != null ? panel.activeInHierarchy : ui.gameObject.activeInHierarchy;
    }

    private bool IsPickupAcquirePressed()
    {
        if (pickup == null || !pickup.IsPlayerInside)
        {
            return false;
        }

#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }

    private static int CountMutationStacks()
    {
        int total = 0;
        MutationLoadout[] loadouts = FindObjectsByType<MutationLoadout>(FindObjectsInactive.Include);

        for (int i = 0; i < loadouts.Length; i++)
        {
            MutationLoadout loadout = loadouts[i];
            if (loadout == null) continue;

            foreach (MutationType mutationType in System.Enum.GetValues(typeof(MutationType)))
            {
                total += Mathf.Max(0, loadout.GetStackCount(mutationType));
            }
        }

        return total;
    }
}

public sealed class RuntimeGameIntegrationRunner : MonoBehaviour
{
    private const string RuntimeCanvasName = "ChaeWoonRuntimeUI";
    private const string CombatHudPrefabPath = "Assets/ChaeWoon/Prefabs/UI/CombatHUDView.prefab";
    private const string BossHpBarPrefabPath = "Assets/ChaeWoon/Prefabs/UI/BossHpBarView.prefab";
    private const string StatUpgradePrefabPath = "Assets/ChaeWoon/Prefabs/UI/CharacterStatUpgradeUI.prefab";

    private static RoomClearMutationRewardSystem rewardSystem;

    private float _nextScanTime;

    public static void IntegrateScene()
    {
        EnsureRewardIntegration();
        EnsurePlayerIntegration();
        EnsureEnemyIntegration();
        EnsureBossIntegration();
        EnsureRuntimeUi();
        EnsureInitialStatUiHidden();
        EnsureMutationPickupGuards();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextScanTime) return;

        _nextScanTime = Time.unscaledTime + 1f;
        IntegrateScene();
    }

    private static void EnsurePlayerIntegration()
    {
        GameObject player = FindPlayerRoot();
        if (player == null) return;

        if (!player.CompareTag("Player") && TagExists("Player"))
            player.tag = "Player";

        Rigidbody2D playerRigidbody = EnsureComponent<Rigidbody2D>(player);
        playerRigidbody.gravityScale = 0f;
        playerRigidbody.freezeRotation = true;

        if (player.GetComponent<Collider2D>() == null)
            player.AddComponent<BoxCollider2D>();

        EnsureComponent<PlayerStatManager>(player);
        EnsureComponent<PlayerMove>(player);
        EnsureComponent<PlayerAttack>(player);
        EnsureComponent<Health>(player);
        EnsureComponent<PlayerHealth>(player);
        EnsureComponent<PlayerTeamCompatibilityBridge>(player);

        if (player.GetComponent<PlayerSkillInputAdapter>() == null &&
            player.GetComponentInChildren<SkillSwitcher>(true) != null)
        {
            player.AddComponent<PlayerSkillInputAdapter>();
        }

        SkillMutationLoadoutBinder loadoutBinder = player.GetComponent<SkillMutationLoadoutBinder>();
        if (loadoutBinder == null && player.GetComponentInChildren<SkillBase>(true) != null)
        {
            loadoutBinder = player.AddComponent<SkillMutationLoadoutBinder>();
            loadoutBinder.Rebuild();
        }
    }

    private static void EnsureRewardIntegration()
    {
        if (rewardSystem != null)
            return;

        rewardSystem = Object.FindFirstObjectByType<RoomClearMutationRewardSystem>(FindObjectsInactive.Include);
        if (rewardSystem != null)
            return;

        GameObject rewardSystemObject = new GameObject("[Room Clear Mutation Reward System]");
        Object.DontDestroyOnLoad(rewardSystemObject);
        rewardSystem = rewardSystemObject.AddComponent<RoomClearMutationRewardSystem>();
    }

    private static void EnsureInitialStatUiHidden()
    {
        if (RoomClearMutationRewardSystem.IsStatRewardActive)
            return;

        CharacterStatUpgradeUI[] statUis =
            Object.FindObjectsByType<CharacterStatUpgradeUI>(FindObjectsInactive.Include);

        foreach (CharacterStatUpgradeUI statUi in statUis)
        {
            if (statUi == null)
                continue;

            SetPrivateField(statUi, "showOnStart", false);
            statUi.Hide();
        }
    }

    private static void EnsureMutationPickupGuards()
    {
        MutationPickup[] pickups = Object.FindObjectsByType<MutationPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MutationPickup pickup in pickups)
        {
            if (pickup == null) continue;
            EnsureComponent<MutationPickupRuntimeGuard>(pickup.gameObject);
        }
    }

    private static void EnsureEnemyIntegration()
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null) continue;
            EnsureComponent<JunMoEnemyCombatAdapter>(enemy.gameObject);
        }
    }

    private static void EnsureBossIntegration()
    {
        BossBase[] bosses = Object.FindObjectsByType<BossBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (BossBase boss in bosses)
        {
            if (boss == null) continue;
            EnsureComponent<JunMoBossCombatAdapter>(boss.gameObject);
        }
    }

    private static void EnsureRuntimeUi()
    {
        Canvas canvas = EnsureRuntimeCanvas();
        if (canvas == null)
            return;

        EnsureEventSystem();
        EnsurePrefabUi<HpBarView>("CombatHUDView", CombatHudPrefabPath, canvas.transform);
        BossHpBarUI bossHpBar = EnsurePrefabUi<BossHpBarUI>("BossHpBarView", BossHpBarPrefabPath, canvas.transform);
        CharacterStatUpgradeUI statUi = EnsurePrefabUi<CharacterStatUpgradeUI>("CharacterStatUpgradeUI", StatUpgradePrefabPath, canvas.transform);
        MutationSelectUI mutationSelectUi = EnsureMutationSelectUi(canvas.transform);
        EnsureMutationDescriptionUi(canvas.transform);

        PlayerStatManager statManager = Object.FindAnyObjectByType<PlayerStatManager>(FindObjectsInactive.Include);
        if (statUi != null && statManager != null)
        {
            statUi.SetPlayerStatManager(statManager);
        }

        SkillMutationLoadoutBinder loadoutBinder =
            Object.FindAnyObjectByType<SkillMutationLoadoutBinder>(FindObjectsInactive.Include);
        if (mutationSelectUi != null && loadoutBinder != null)
        {
            SetPrivateField(mutationSelectUi, "mutationLoadoutBinder", loadoutBinder);
        }

        BossBase activeBoss = FindActiveBoss();
        if (bossHpBar != null && activeBoss != null && !bossHpBar.HasLiveBoss)
        {
            bossHpBar.SetBoss(activeBoss);
        }
    }

    private static Canvas EnsureRuntimeCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas existingCanvas = canvases[i];
            if (existingCanvas != null && existingCanvas.isActiveAndEnabled && existingCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return existingCanvas;
        }

        GameObject canvasObject = new(RuntimeCanvasName);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            return;

        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private static T EnsurePrefabUi<T>(string instanceName, string prefabPath, Transform parent) where T : Component
    {
        T existing = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject instance = InstantiateEditorPrefab(prefabPath, parent);
        if (instance == null)
            return null;

        instance.name = instanceName;
        return instance.GetComponentInChildren<T>(true);
    }

    private static GameObject InstantiateEditorPrefab(string prefabPath, Transform parent)
    {
#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return null;

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return null;

        instance.transform.SetParent(parent, false);
        return instance;
#else
        return null;
#endif
    }

    private static MutationSelectUI EnsureMutationSelectUi(Transform parent)
    {
        MutationSelectUI existing = Object.FindAnyObjectByType<MutationSelectUI>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject root = CreateUiObject("MutationSelectUIRoot", parent);
        root.SetActive(false);

        GameObject panel = CreatePanel("MutationSelectPanel", root.transform, new Vector2(760f, 560f));
        TextMeshProUGUI titleText = CreateText("TitleText", panel.transform, "변이할 공격을 선택하시오", 34f);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -42f);
        titleRect.sizeDelta = new Vector2(-60f, 56f);

        SkillSelectSlotUI[] slots = new SkillSelectSlotUI[4];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = CreateMutationSlot(panel.transform, i);
        }

        MutationSelectUI ui = root.AddComponent<MutationSelectUI>();
        SetPrivateField(ui, "panel", panel);
        SetPrivateField(ui, "titleText", titleText);
        SetPrivateField(ui, "skillSlots", slots);
        SetPrivateField(ui, "hideAfterSelection", true);

        root.SetActive(true);
        return ui;
    }

    private static SkillSelectSlotUI CreateMutationSlot(Transform parent, int index)
    {
        GameObject slotObject = CreatePanel($"SkillSlot_{index + 1}", parent, new Vector2(330f, 180f));
        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        slotRect.anchorMin = slotRect.anchorMax = new Vector2(index % 2 == 0 ? 0.28f : 0.72f, index < 2 ? 0.62f : 0.26f);
        slotRect.anchoredPosition = Vector2.zero;

        Button button = slotObject.AddComponent<Button>();
        TextMeshProUGUI nameText = CreateText("SkillNameText", slotObject.transform, string.Empty, 24f);
        TextMeshProUGUI descriptionText = CreateText("SkillDescriptionText", slotObject.transform, string.Empty, 18f);
        TextMeshProUGUI mutationText = CreateText("SkillMutationListText", slotObject.transform, string.Empty, 17f);

        nameText.rectTransform.anchoredPosition = new Vector2(0f, 54f);
        descriptionText.rectTransform.anchoredPosition = new Vector2(0f, 12f);
        mutationText.rectTransform.anchoredPosition = new Vector2(0f, -50f);

        SkillSelectSlotUI slot = slotObject.AddComponent<SkillSelectSlotUI>();
        SetPrivateField(slot, "skillNameText", nameText);
        SetPrivateField(slot, "skillDescriptionText", descriptionText);
        SetPrivateField(slot, "mutationListText", mutationText);
        SetPrivateField(slot, "selectButton", button);
        return slot;
    }

    private static MutationDescriptionUI EnsureMutationDescriptionUi(Transform parent)
    {
        MutationDescriptionUI existing = Object.FindAnyObjectByType<MutationDescriptionUI>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject root = CreateUiObject("MutationDescriptionUIRoot", parent);
        root.SetActive(false);

        GameObject panel = CreatePanel("MutationDescriptionPanel", root.transform, new Vector2(420f, 220f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.14f);

        TextMeshProUGUI gradeText = CreateText("GradeText", panel.transform, string.Empty, 20f);
        TextMeshProUGUI nameText = CreateText("NameText", panel.transform, string.Empty, 26f);
        TextMeshProUGUI descriptionText = CreateText("DescriptionText", panel.transform, string.Empty, 18f);
        gradeText.rectTransform.anchoredPosition = new Vector2(0f, 76f);
        nameText.rectTransform.anchoredPosition = new Vector2(0f, 38f);
        descriptionText.rectTransform.anchoredPosition = new Vector2(0f, -36f);

        MutationDescriptionUI ui = root.AddComponent<MutationDescriptionUI>();
        SetPrivateField(ui, "panel", panel);
        SetPrivateField(ui, "gradeText", gradeText);
        SetPrivateField(ui, "nameText", nameText);
        SetPrivateField(ui, "descriptionText", descriptionText);

        root.SetActive(true);
        return ui;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject obj = new(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return obj;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.04f, 0.04f, 0.04f, 0.92f);
        return panel;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300f, 44f);
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static BossBase FindActiveBoss()
    {
        BossBase[] bosses = Object.FindObjectsByType<BossBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < bosses.Length; i++)
        {
            BossBase boss = bosses[i];
            if (boss != null && boss.gameObject.activeInHierarchy && !boss.IsDead)
                return boss;
        }

        return null;
    }

    private static GameObject FindPlayerRoot()
    {
        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged;

        PlayerHealth health = Object.FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        if (health != null) return health.gameObject;

        PlayerMove move = Object.FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);
        if (move != null) return move.gameObject;

        PlayerStatManager stats = Object.FindFirstObjectByType<PlayerStatManager>(FindObjectsInactive.Include);
        if (stats != null) return stats.gameObject;

        SkillSwitcher switcher = Object.FindFirstObjectByType<SkillSwitcher>(FindObjectsInactive.Include);
        if (switcher != null)
            return switcher.transform.root != null ? switcher.transform.root.gameObject : switcher.gameObject;

        return null;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null)
            return;

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            return;

        field.SetValue(target, value);
    }

    private static bool TagExists(string tagName)
    {
        try
        {
            GameObject.FindGameObjectsWithTag(tagName);
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }
}
