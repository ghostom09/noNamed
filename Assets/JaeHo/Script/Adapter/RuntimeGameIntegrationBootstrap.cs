using BossSystem.Boss;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
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
    private static RoomClearMutationRewardSystem rewardSystem;

    private float _nextScanTime;

    public static void IntegrateScene()
    {
        EnsureRewardIntegration();
        EnsureMutationPickupGuards();
        EnsurePlayerIntegration();
        EnsureEnemyIntegration();
        EnsureBossIntegration();
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
