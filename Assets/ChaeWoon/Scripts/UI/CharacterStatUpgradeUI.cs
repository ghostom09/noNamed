using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterStatUpgradeUI : MonoBehaviour
{
    private const string PlayerStatManagerTypeName = "PlayerStatManager";
    private const string TitleLabel = "status";

    private static readonly StatBinding[] StatBindings =
    {
        new("\uccb4\ub825", "Health", "health", string.Empty, 6f, 1f),
        new("\uc6d0\uac70\ub9ac \ub370\ubbf8\uc9c0", "RangedDamage", "rangedDamage", string.Empty, 6f, 1f),
        new("\uadfc\uac70\ub9ac \ub370\ubbf8\uc9c0", "MeleeDamage", "meleeDamage", string.Empty, 6f, 1f),
        new("\uacf5\uaca9\uc18d\ub3c4", "AttackSpeed", "attackSpeed", string.Empty, 6f, 1f),
        new("\uc774\ub3d9 \uc18d\ub3c4", "MoveSpeed", "moveSpeed", string.Empty, 6f, 1f),
        new("\uce58\uba85\ud0c0\uc728", "CriticalProbability", "criticalProbability", string.Empty, 6f, 1f),
        new("\uc2a4\ud0ac \ubc94\uc704", "SkillArea", "skillArea", string.Empty, 6f, 1f),
    };

    [Header("Runtime Binding")]
    [SerializeField] private Component playerStatManager;
    [SerializeField] private HpBarView hpBarView;
    [SerializeField] private bool autoFindPlayerStatManager = true;
    [SerializeField] private bool useFallbackPreviewValues = true;

    [Header("Backend")]
    [SerializeField, Min(0)] private int availablePoints = 5;
    [SerializeField] private bool useLevelUpFallback;
    [SerializeField] private StatUpgradeConfirmedEvent onStatUpgradeConfirmed = new();

    [Header("View")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text pointText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private StatRowView[] rows = new StatRowView[0];
    [SerializeField] private bool showOnStart = true;

    [Header("Refresh")]
    [SerializeField] private bool refreshContinuously = true;
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;

    public event Action<string, int> StatUpgradeConfirmed;

    private readonly int[] pendingUpgradeCounts = new int[StatBindings.Length];
    private float nextRefreshTime;
    private int lastLevel = -1;
    private string statusMessage;

    private void Awake()
    {
        BindRuntimeReferences();
        ConfigureButtons();
    }

    private void OnEnable()
    {
        if (panel != null)
        {
            panel.SetActive(showOnStart);
        }

        Refresh();
    }

    private void Update()
    {
        if (!refreshContinuously || Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + refreshInterval;
        Refresh();
    }

    public void SetPlayerStatManager(Component target)
    {
        playerStatManager = target;
        Refresh();
    }

    public void SetHpBarView(HpBarView target)
    {
        hpBarView = target;
        Refresh();
    }

    public void Show()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        Refresh();
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void Toggle()
    {
        if (panel == null)
        {
            return;
        }

        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        BindRuntimeReferences();

        SetText(titleText, TitleLabel);

        int selectedTotal = GetPendingUpgradeTotal();
        SetText(pointText, $"point: {Mathf.Max(0, availablePoints - selectedTotal)}");

        int level = ReadLevel(playerStatManager);

        for (int i = 0; i < StatBindings.Length; i++)
        {
            RefreshRow(i, level);
        }

        SetText(feedbackText, BuildFeedbackText());
        SetConfirmInteractable(selectedTotal > 0);

        SyncHpBar(level);
        lastLevel = level;
    }

    public void SelectStatUpgrade(int statIndex)
    {
        if (statIndex < 0 || statIndex >= pendingUpgradeCounts.Length)
        {
            return;
        }

        if (GetPendingUpgradeTotal() >= availablePoints)
        {
            statusMessage = "\uc0ac\uc6a9 \uac00\ub2a5\ud55c \ud3ec\uc778\ud2b8\uac00 \uc5c6\uc2b5\ub2c8\ub2e4.";
            Refresh();
            return;
        }

        pendingUpgradeCounts[statIndex]++;
        statusMessage = string.Empty;
        Refresh();
    }

    public void CancelSelection()
    {
        Array.Clear(pendingUpgradeCounts, 0, pendingUpgradeCounts.Length);
        statusMessage = string.Empty;
        Refresh();
    }

    public void Confirm()
    {
        int selectedTotal = GetPendingUpgradeTotal();
        if (selectedTotal <= 0)
        {
            statusMessage = "\uc120\ud0dd\ud55c \uc2a4\ud0ef\uc774 \uc5c6\uc2b5\ub2c8\ub2e4.";
            Refresh();
            return;
        }

        BindRuntimeReferences();

        bool appliedToBackend = false;
        for (int i = 0; i < StatBindings.Length; i++)
        {
            int count = pendingUpgradeCounts[i];
            if (count <= 0)
            {
                continue;
            }

            StatBinding binding = StatBindings[i];
            onStatUpgradeConfirmed.Invoke(binding.AssetFieldName, count);
            StatUpgradeConfirmed?.Invoke(binding.AssetFieldName, count);
            appliedToBackend |= TryInvokeStatUpgrade(binding, count);
        }

        if (!appliedToBackend && useLevelUpFallback)
        {
            appliedToBackend = TryInvokeLevelUpFallback(selectedTotal);
        }

        Array.Clear(pendingUpgradeCounts, 0, pendingUpgradeCounts.Length);
        statusMessage = appliedToBackend
            ? "\uac15\ud654 \uc801\uc6a9 \uc644\ub8cc"
            : "\uc120\ud0dd \ub0b4\uc5ed \ud655\uc815 \uc644\ub8cc";
        Refresh();
    }

    public void Upgrade()
    {
        Confirm();
    }

    private void RefreshRow(int index, int level)
    {
        if (index < 0 || index >= rows.Length || rows[index] == null)
        {
            return;
        }

        StatBinding binding = StatBindings[index];
        StatRowView row = rows[index];
        ResolveStatValues(binding, level, out float currentValue, out float stepValue);

        int pendingCount = pendingUpgradeCounts[index];
        int previewCount = Mathf.Max(1, pendingCount);
        float previewValue = currentValue + stepValue * previewCount;

        string currentText = FormatValue(currentValue, binding.Suffix);
        string line = pendingCount > 0
            ? $"{binding.Label,-9} {currentText,4}  <size=85%>\u25ba</size>  <color=#d7ff32>{FormatValue(previewValue, binding.Suffix)}</color>        +"
            : $"{binding.Label,-9} {currentText,4}              +";
        SetText(row.LineText, line);

        if (row.PlusButton != null)
        {
            row.PlusButton.interactable = playerStatManager != null || useFallbackPreviewValues;
        }
    }

    private void ResolveStatValues(StatBinding binding, int level, out float currentValue, out float stepValue)
    {
        currentValue = binding.FallbackCurrentValue;
        stepValue = binding.FallbackStepValue;

        bool hasCurrent = false;
        if (playerStatManager != null)
        {
            hasCurrent = TryReadFloatProperty(playerStatManager, binding.ManagerPropertyName, out currentValue);
        }

        ScriptableObject statAsset = ResolvePlayerStatAsset(playerStatManager);
        if (!hasCurrent && statAsset != null)
        {
            hasCurrent = TryCalculateAssetStat(statAsset, binding.AssetFieldName, level, out currentValue);
        }

        if (statAsset != null &&
            TryCalculateAssetStat(statAsset, binding.AssetFieldName, level + 1, out float nextValue))
        {
            stepValue = nextValue - currentValue;
        }
        else if (!hasCurrent && !useFallbackPreviewValues)
        {
            currentValue = 0f;
            stepValue = 0f;
        }
    }

    private string BuildFeedbackText()
    {
        if (!string.IsNullOrEmpty(statusMessage))
        {
            return statusMessage;
        }

        if (playerStatManager == null && useFallbackPreviewValues)
        {
            return "PlayerStatManager\ub97c \ucc3e\uc9c0 \ubabb\ud588\uc2b5\ub2c8\ub2e4. \ubbf8\ub9ac\ubcf4\uae30 \uac12\uc73c\ub85c \ud45c\uc2dc\ud569\ub2c8\ub2e4.";
        }

        return string.Empty;
    }

    private void BindRuntimeReferences()
    {
        if (hpBarView == null)
        {
            hpBarView = FindAnyObjectByType<HpBarView>();
        }

        if (playerStatManager != null || !autoFindPlayerStatManager)
        {
            return;
        }

        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour.GetType().Name != PlayerStatManagerTypeName)
            {
                continue;
            }

            playerStatManager = behaviour;
            break;
        }
    }

    private void ConfigureButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(Confirm);
            confirmButton.onClick.AddListener(Confirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CancelSelection);
            cancelButton.onClick.AddListener(CancelSelection);
        }

        int rowCount = Mathf.Min(rows.Length, StatBindings.Length);
        for (int i = 0; i < rowCount; i++)
        {
            if (rows[i]?.PlusButton == null)
            {
                continue;
            }

            int capturedIndex = i;
            rows[i].PlusButton.onClick.AddListener(() => SelectStatUpgrade(capturedIndex));
        }
    }

    private void SyncHpBar(int level)
    {
        if (hpBarView == null || playerStatManager == null || level <= 0)
        {
            return;
        }

        ScriptableObject statAsset = ResolvePlayerStatAsset(playerStatManager);
        if (statAsset == null || level == lastLevel)
        {
            return;
        }

        hpBarView.SetPlayerStat(statAsset, level);
    }

    private bool TryInvokeStatUpgrade(StatBinding binding, int count)
    {
        if (playerStatManager == null)
        {
            return false;
        }

        string[] methodNames =
        {
            "UpgradeStat",
            "UpgradePlayerStat",
            "IncreaseStat",
            "AddStatUpgrade",
        };

        Type type = playerStatManager.GetType();
        foreach (string methodName in methodNames)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                if (method.Name != methodName)
                {
                    continue;
                }

                if (TryInvokeStatUpgradeMethod(method, binding.AssetFieldName, count) ||
                    TryInvokeStatUpgradeMethod(method, binding.ManagerPropertyName, count))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryInvokeStatUpgradeMethod(MethodInfo method, string statKey, int count)
    {
        ParameterInfo[] parameters = method.GetParameters();

        try
        {
            if (parameters.Length == 2 &&
                parameters[0].ParameterType == typeof(string) &&
                parameters[1].ParameterType == typeof(int))
            {
                method.Invoke(playerStatManager, new object[] { statKey, count });
                return true;
            }

            if (parameters.Length == 2 &&
                parameters[0].ParameterType == typeof(string) &&
                parameters[1].ParameterType == typeof(float))
            {
                method.Invoke(playerStatManager, new object[] { statKey, (float)count });
                return true;
            }

            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
            {
                for (int i = 0; i < count; i++)
                {
                    method.Invoke(playerStatManager, new object[] { statKey });
                }

                return true;
            }
        }
        catch (TargetInvocationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        return false;
    }

    private bool TryInvokeLevelUpFallback(int count)
    {
        if (playerStatManager == null || count <= 0)
        {
            return false;
        }

        MethodInfo levelUpMethod = FindMethod(playerStatManager.GetType(), "LevelUp");
        if (levelUpMethod == null)
        {
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            levelUpMethod.Invoke(playerStatManager, null);
        }

        return true;
    }

    private int GetPendingUpgradeTotal()
    {
        int total = 0;
        for (int i = 0; i < pendingUpgradeCounts.Length; i++)
        {
            total += pendingUpgradeCounts[i];
        }

        return total;
    }

    private void SetConfirmInteractable(bool interactable)
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = interactable;
        }
    }

    private static bool TryReadFloatProperty(Component target, string propertyName, out float value)
    {
        value = 0f;
        if (target == null)
        {
            return false;
        }

        PropertyInfo property = FindProperty(target.GetType(), propertyName);
        if (property == null)
        {
            return false;
        }

        object rawValue = property.GetValue(target);
        return TryConvertToFloat(rawValue, out value);
    }

    private static int ReadLevel(Component target)
    {
        if (target == null)
        {
            return -1;
        }

        PropertyInfo property = FindProperty(target.GetType(), "Level");
        if (property == null)
        {
            return -1;
        }

        object rawValue = property.GetValue(target);
        return rawValue is int level ? level : -1;
    }

    private static ScriptableObject ResolvePlayerStatAsset(Component target)
    {
        if (target == null)
        {
            return null;
        }

        FieldInfo field = FindField(target.GetType(), "playerStat");
        return field?.GetValue(target) as ScriptableObject;
    }

    private static bool TryCalculateAssetStat(ScriptableObject statAsset, string baseFieldName, int level, out float value)
    {
        value = 0f;
        if (statAsset == null || level <= 0)
        {
            return false;
        }

        Type statType = statAsset.GetType();
        FieldInfo baseField = FindField(statType, baseFieldName);
        if (baseField == null || !TryConvertToFloat(baseField.GetValue(statAsset), out float baseValue))
        {
            return false;
        }

        value = baseValue;

        MethodInfo bonusMethod = FindMethod(statType, "GetLevelBonus");
        if (bonusMethod == null)
        {
            return true;
        }

        object bonus = bonusMethod.Invoke(statAsset, new object[] { level });
        if (bonus == null)
        {
            return true;
        }

        FieldInfo bonusField = FindField(bonus.GetType(), baseFieldName);
        if (bonusField != null && TryConvertToFloat(bonusField.GetValue(bonus), out float bonusValue))
        {
            value += bonusValue;
        }

        return true;
    }

    private static void SetText(TMP_Text textComponent, string value)
    {
        if (textComponent != null)
        {
            textComponent.text = value;
        }
    }

    private static string FormatValue(float value, string suffix)
    {
        return $"{value:0.##}{suffix}";
    }

    private static bool TryConvertToFloat(object value, out float result)
    {
        result = 0f;
        if (value is float floatValue)
        {
            result = floatValue;
            return true;
        }

        if (value is int intValue)
        {
            result = intValue;
            return true;
        }

        if (value is double doubleValue)
        {
            result = (float)doubleValue;
            return true;
        }

        return false;
    }

    private static FieldInfo FindField(Type type, string fieldName)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field;
            }

            type = type.BaseType;
        }

        return null;
    }

    private static PropertyInfo FindProperty(Type type, string propertyName)
    {
        while (type != null)
        {
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property;
            }

            type = type.BaseType;
        }

        return null;
    }

    private static MethodInfo FindMethod(Type type, string methodName)
    {
        while (type != null)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                return method;
            }

            type = type.BaseType;
        }

        return null;
    }

    [Serializable]
    public sealed class StatUpgradeConfirmedEvent : UnityEvent<string, int>
    {
    }

    [Serializable]
    private sealed class StatRowView
    {
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private Button plusButton;

        public TMP_Text LineText => lineText;
        public Button PlusButton => plusButton;
    }

    private readonly struct StatBinding
    {
        public StatBinding(
            string label,
            string managerPropertyName,
            string assetFieldName,
            string suffix,
            float fallbackCurrentValue,
            float fallbackStepValue)
        {
            Label = label;
            ManagerPropertyName = managerPropertyName;
            AssetFieldName = assetFieldName;
            Suffix = suffix;
            FallbackCurrentValue = fallbackCurrentValue;
            FallbackStepValue = fallbackStepValue;
        }

        public string Label { get; }
        public string ManagerPropertyName { get; }
        public string AssetFieldName { get; }
        public string Suffix { get; }
        public float FallbackCurrentValue { get; }
        public float FallbackStepValue { get; }
    }
}
