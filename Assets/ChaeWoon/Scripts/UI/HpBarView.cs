using TMPro;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HpBarView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const string HpFillName = "HpFill";
    private const string HpBackgroundName = "HpBackground";
    private const string HpTextName = "HpText";
    private const string StatPanelName = "StatPanel";
    private const string StatTextPath = "StatPanel/StatText";

    private Image hpFillImage;
    private Image hpBackgroundImage;
    private TextMeshProUGUI hpText;
    private GameObject statPanel;
    private RectTransform statPanelRect;
    private Image statPanelImage;
    private TextMeshProUGUI statText;
    private CanvasGroup statTextCanvasGroup;
    private ScriptableObject playerStat;
    private PlayerStatManager statManager;
    private int level = 1;

    [SerializeField] private bool autoBindRuntimeHealth = true;
    [SerializeField] private bool autoBindPlayerStats = true;
    [Header("HP Border")]
    [SerializeField] private Color hpBorderColor = new(0.08f, 0.08f, 0.08f, 1f);
    [SerializeField] private Vector2 hpBorderPadding = new(6f, 6f);

    [Header("Stat Panel Hover")]
    [SerializeField] private Vector2 collapsedStatPanelPadding = new(40f, 42f);
    [SerializeField] private Vector2 expandedStatPanelSize;
    [SerializeField, Min(0.01f)] private float statPanelAnimationDuration = 0.2f;
    [SerializeField] private AnimationCurve statPanelEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float statTextTopOffset = 124f;

    private Vector2 collapsedStatPanelSize;
    private Vector2 resolvedExpandedStatPanelSize;
    private Vector2 initialExpandedStatPanelSize;
    private float statPanelProgress;
    private bool wantsStatPanelExpanded;
    private bool statPanelLayoutInitialized;
    private bool hasInitialExpandedStatPanelSize;

    private void Awake()
    {
        BindHpReferences();
        BindStatPanelReferences();
        ApplyHpBorder();
        InitializeStatPanelLayout();
        RefreshStatText();
        SetStatPanelExpanded(false, true);
    }

    private void Start()
    {
        EnsureRuntimeHealthBinder();
        EnsureStatPanelBinder();
    }

    private void Update()
    {
        AnimateStatPanel();
    }

    public void SetHp(float currentHp, float maxHp)
    {
        BindHpReferences();

        if (hpFillImage == null || hpText == null)
        {
            Debug.LogWarning("[HpBarView] HP UI references are missing.", this);
            return;
        }

        if (maxHp <= 0f)
        {
            hpFillImage.fillAmount = 0f;
            hpText.text = "0 / 0";
            return;
        }

        currentHp = Mathf.Clamp(currentHp, 0f, maxHp);

        float ratio = currentHp / maxHp;
        hpFillImage.fillAmount = Mathf.Clamp01(ratio);

        hpText.text = $"{currentHp:0} / {maxHp:0}";
    }

    public void SetPlayerStat(ScriptableObject stat, int statLevel)
    {
        playerStat = stat;
        level = Mathf.Max(1, statLevel);
        RefreshStatText();
    }

    public void ClearPlayerStat()
    {
        playerStat = null;
        level = 1;
        RefreshStatText();
        HideStatPanel();
    }

    public void SetLevel(int statLevel)
    {
        level = Mathf.Max(1, statLevel);
        RefreshStatText();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowStatPanel();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideStatPanel();
    }

    private void ShowStatPanel()
    {
        BindStatPanelReferences();
        InitializeStatPanelLayout();
        RefreshStatText();
        SetStatPanelExpanded(true, false);
    }

    private void HideStatPanel()
    {
        BindStatPanelReferences();
        InitializeStatPanelLayout();
        SetStatPanelExpanded(false, false);
    }

    private void RefreshStatText()
    {
        BindStatPanelReferences();

        if (statText == null)
        {
            return;
        }

        if (playerStat != null)
        {
            statText.text = BuildStatText();
        }
        else if (statManager != null)
        {
            statText.text = BuildStatTextFromManager(statManager);
        }
        else
        {
            statText.text = string.Empty;
        }
    }

    private string BuildStatText()
    {
        object bonus = GetLevelBonus();

        float meleeDamage = GetStatValue("meleeDamage") + GetBonusValue(bonus, "meleeDamage");
        float rangedDamage = GetStatValue("rangedDamage") + GetBonusValue(bonus, "rangedDamage");
        float criticalProbability = GetStatValue("criticalProbability") + GetBonusValue(bonus, "criticalProbability");
        float moveSpeed = GetStatValue("moveSpeed") + GetBonusValue(bonus, "moveSpeed");
        float attackSpeed = GetStatValue("attackSpeed") + GetBonusValue(bonus, "attackSpeed");
        float skillArea = GetStatValue("skillArea") + GetBonusValue(bonus, "skillArea");

        return $"{playerStat.name}\n" +
               $"LV    {level}\n" +
               $"RANGE {rangedDamage:0.#}\n" +
               $"MELEE {meleeDamage:0.#}\n" +
               $"ASPD  {attackSpeed:0.##}\n" +
               $"MOVE  {moveSpeed:0.##}\n" +
               $"CRIT  {criticalProbability:0.#}%\n" +
               $"AREA  {skillArea:0.##}";
    }

    public void RefreshFromManager(PlayerStatManager manager)
    {
        statManager = manager;
        BindStatPanelReferences();
        RefreshStatText();
    }

    private static string BuildStatTextFromManager(PlayerStatManager m)
    {
        return $"Player\n" +
               $"LV    {m.Level}\n" +
               $"RANGE {m.RangedDamage:0.#}\n" +
               $"MELEE {m.MeleeDamage:0.#}\n" +
               $"ASPD  {m.AttackSpeed:0.##}\n" +
               $"MOVE  {m.MoveSpeed:0.##}\n" +
               $"CRIT  {m.CriticalProbability:0.#}%\n" +
               $"AREA  {m.SkillArea:0.##}";
    }


    private object GetLevelBonus()
    {
        if (playerStat == null)
        {
            return null;
        }

        MethodInfo getLevelBonus = playerStat.GetType().GetMethod("GetLevelBonus", new[] { typeof(int) });
        return getLevelBonus == null ? null : getLevelBonus.Invoke(playerStat, new object[] { level });
    }

    private float GetStatValue(string fieldName)
    {
        if (playerStat == null)
        {
            return 0f;
        }

        FieldInfo field = playerStat.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        return field != null && field.GetValue(playerStat) is float value ? value : 0f;
    }

    private static float GetBonusValue(object bonus, string fieldName)
    {
        if (bonus == null)
        {
            return 0f;
        }

        FieldInfo field = bonus.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        return field != null && field.GetValue(bonus) is float value ? value : 0f;
    }

    private void BindHpReferences()
    {
        if (hpFillImage == null)
        {
            hpFillImage = FindChildComponent<Image>(HpFillName);
        }

        if (hpBackgroundImage == null)
        {
            hpBackgroundImage = FindChildComponent<Image>(HpBackgroundName);
        }

        if (hpText == null)
        {
            hpText = FindChildComponent<TextMeshProUGUI>(HpTextName);
        }
    }

    private void BindStatPanelReferences()
    {
        if (statPanel == null)
        {
            Transform panel = transform.Find(StatPanelName);

            if (panel == null)
            {
                Debug.LogWarning($"[HpBarView] Child '{StatPanelName}' was not found.", this);
                return;
            }

            statPanel = panel.gameObject;
        }

        if (statPanelRect == null && statPanel != null)
        {
            statPanelRect = statPanel.GetComponent<RectTransform>();
        }

        if (statPanelImage == null && statPanel != null)
        {
            statPanelImage = statPanel.GetComponent<Image>();
        }

        if (statText == null)
        {
            statText = FindChildComponent<TextMeshProUGUI>(StatTextPath);
        }

        if (statTextCanvasGroup == null && statText != null)
        {
            statTextCanvasGroup = statText.GetComponent<CanvasGroup>();
        }
    }

    private T FindChildComponent<T>(string childPath) where T : Component
    {
        Transform child = transform.Find(childPath);

        if (child == null)
        {
            Debug.LogWarning($"[HpBarView] Child '{childPath}' was not found.", this);
            return null;
        }

        T component = child.GetComponent<T>();

        if (component == null)
        {
            Debug.LogWarning($"[HpBarView] Child '{childPath}' does not have {typeof(T).Name}.", child);
        }

        return component;
    }

    private void EnsureRuntimeHealthBinder()
    {
        if (!autoBindRuntimeHealth || GetComponent<HpBarHealthBinder>() != null)
        {
            return;
        }

        if (SkillMutationLoadoutBinder.FindFirstComponentByTypeName("PlayerHealth") != null ||
            SkillMutationLoadoutBinder.FindFirstComponentByTypeName("Health") != null)
        {
            gameObject.AddComponent<HpBarHealthBinder>();
        }
    }

    private void EnsureStatPanelBinder()
    {
        if (!autoBindPlayerStats || GetComponent<StatPanelBinder>() != null)
        {
            return;
        }

        gameObject.AddComponent<StatPanelBinder>();
    }

    private void ApplyHpBorder()
    {
        BindHpReferences();

        if (hpFillImage == null || hpBackgroundImage == null)
        {
            return;
        }

        RectTransform fillRect = hpFillImage.rectTransform;
        RectTransform backgroundRect = hpBackgroundImage.rectTransform;
        float paddingX = Mathf.Max(0f, hpBorderPadding.x);
        float paddingY = Mathf.Max(0f, hpBorderPadding.y);

        hpBackgroundImage.color = hpBorderColor;
        backgroundRect.anchorMin = fillRect.anchorMin;
        backgroundRect.anchorMax = fillRect.anchorMax;
        backgroundRect.pivot = fillRect.pivot;
        backgroundRect.sizeDelta = fillRect.sizeDelta + new Vector2(paddingX * 2f, paddingY * 2f);
        backgroundRect.anchoredPosition = fillRect.anchoredPosition + new Vector2(
            (fillRect.pivot.x * 2f - 1f) * paddingX,
            (fillRect.pivot.y * 2f - 1f) * paddingY);

        // HpBackground must sit right behind HpFill to look like a border.
        int fillIndex = hpFillImage.transform.GetSiblingIndex();
        hpBackgroundImage.transform.SetSiblingIndex(Mathf.Max(0, fillIndex - 1));
    }

    private void InitializeStatPanelLayout()
    {
        BindHpReferences();
        BindStatPanelReferences();

        if (statPanelRect == null)
        {
            return;
        }

        if (statPanel != null && !statPanel.activeSelf)
        {
            statPanel.SetActive(true);
        }

        if (!hasInitialExpandedStatPanelSize)
        {
            initialExpandedStatPanelSize = statPanelRect.sizeDelta;
            hasInitialExpandedStatPanelSize = true;
        }

        Vector2 hpSize = ResolveHpBarSize();
        collapsedStatPanelSize = new Vector2(
            Mathf.Max(1f, hpSize.x + Mathf.Max(0f, collapsedStatPanelPadding.x)),
            Mathf.Max(1f, hpSize.y + Mathf.Max(0f, collapsedStatPanelPadding.y)));

        resolvedExpandedStatPanelSize = IsZero(expandedStatPanelSize)
            ? initialExpandedStatPanelSize
            : expandedStatPanelSize;

        resolvedExpandedStatPanelSize = new Vector2(
            Mathf.Max(collapsedStatPanelSize.x, resolvedExpandedStatPanelSize.x),
            Mathf.Max(collapsedStatPanelSize.y, resolvedExpandedStatPanelSize.y));

        if (statPanelImage != null)
        {
            statPanelImage.raycastTarget = true;
        }

        if (statText != null)
        {
            RectTransform statTextRect = statText.rectTransform;
            Vector2 textPosition = statTextRect.anchoredPosition;
            textPosition.y = -Mathf.Abs(statTextTopOffset);
            statTextRect.anchoredPosition = textPosition;
        }

        if (statText != null && statTextCanvasGroup == null)
        {
            statTextCanvasGroup = statText.gameObject.AddComponent<CanvasGroup>();
        }

        if (statTextCanvasGroup != null)
        {
            statTextCanvasGroup.interactable = false;
            statTextCanvasGroup.blocksRaycasts = false;
        }

        statPanelLayoutInitialized = true;
    }

    private void SetStatPanelExpanded(bool expanded, bool instant)
    {
        wantsStatPanelExpanded = expanded;

        if (!statPanelLayoutInitialized)
        {
            InitializeStatPanelLayout();
        }

        if (instant)
        {
            statPanelProgress = expanded ? 1f : 0f;
            ApplyStatPanelProgress();
        }
    }

    private void AnimateStatPanel()
    {
        if (!statPanelLayoutInitialized)
        {
            return;
        }

        float targetProgress = wantsStatPanelExpanded ? 1f : 0f;

        if (Mathf.Approximately(statPanelProgress, targetProgress))
        {
            return;
        }

        float duration = Mathf.Max(0.01f, statPanelAnimationDuration);
        statPanelProgress = Mathf.MoveTowards(statPanelProgress, targetProgress, Time.unscaledDeltaTime / duration);
        ApplyStatPanelProgress();
    }

    private void ApplyStatPanelProgress()
    {
        if (statPanelRect == null)
        {
            return;
        }

        if (statPanel != null && !statPanel.activeSelf)
        {
            statPanel.SetActive(true);
        }

        float easedProgress = EvaluateStatPanelEase(statPanelProgress);
        statPanelRect.sizeDelta = Vector2.Lerp(collapsedStatPanelSize, resolvedExpandedStatPanelSize, easedProgress);

        if (statTextCanvasGroup != null)
        {
            statTextCanvasGroup.alpha = easedProgress;
        }

        if (statText != null)
        {
            statText.gameObject.SetActive(easedProgress > 0.01f);
        }
    }

    private Vector2 ResolveHpBarSize()
    {
        if (hpFillImage != null)
        {
            return hpFillImage.rectTransform.sizeDelta;
        }

        if (hpBackgroundImage != null)
        {
            return hpBackgroundImage.rectTransform.sizeDelta;
        }

        return new Vector2(350f, 100f);
    }

    private float EvaluateStatPanelEase(float progress)
    {
        if (statPanelEase == null || statPanelEase.length == 0)
        {
            return progress;
        }

        return Mathf.Clamp01(statPanelEase.Evaluate(Mathf.Clamp01(progress)));
    }

    private static bool IsZero(Vector2 value)
    {
        return value.sqrMagnitude <= 0.0001f;
    }
}
