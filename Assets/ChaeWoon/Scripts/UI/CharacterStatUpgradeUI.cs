using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterStatUpgradeUI : MonoBehaviour
{
    private const string TitleLabel = "status";

    private static readonly StatBinding[] StatBindings =
    {
        new("체력", PlayerStatType.Health, string.Empty, 6f, 1f),
        new("원거리 데미지", PlayerStatType.RangedDamage, string.Empty, 6f, 1f),
        new("근거리 데미지", PlayerStatType.MeleeDamage, string.Empty, 6f, 1f),
        new("공격속도", PlayerStatType.AttackSpeed, string.Empty, 6f, 1f),
        new("이동 속도", PlayerStatType.MoveSpeed, string.Empty, 6f, 1f),
        new("치명타율", PlayerStatType.CriticalProbability, string.Empty, 6f, 1f),
        new("스킬 범위", PlayerStatType.SkillArea, string.Empty, 6f, 1f),
    };

    [Header("Runtime Binding")]
    [SerializeField] private PlayerStatManager playerStatManager;
    [SerializeField] private bool autoFindPlayerStatManager = true;
    [SerializeField] private bool useFallbackPreviewValues = true;

    [Header("Backend")]
    [Tooltip("PlayerStatManager가 없을 때만 사용하는 예비 포인트 잔고.")]
    [SerializeField, Min(0)] private int availablePoints = 5;
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
    private float nextRuntimeSearchTime;
    private string statusMessage;
    private PlayerStatManager subscribedManager;
    private bool isConfirming;

    private int CurrentAvailablePoints =>
        playerStatManager != null ? playerStatManager.AvailablePoints : availablePoints;

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

        if (showOnStart)
        {
            BringToFront();
        }

        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
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

    public void SetPlayerStatManager(PlayerStatManager target)
    {
        playerStatManager = target;
        Refresh();
    }

    public void Show()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        BringToFront();
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
            BringToFront();
            Refresh();
        }
    }

    public void Refresh()
    {
        BindRuntimeReferences();

        SetText(titleText, TitleLabel);

        int selectedTotal = GetPendingUpgradeTotal();
        SetText(pointText, $"point: {Mathf.Max(0, CurrentAvailablePoints - selectedTotal)}");

        for (int i = 0; i < StatBindings.Length; i++)
        {
            RefreshRow(i);
        }

        SetText(feedbackText, BuildFeedbackText());
        SetConfirmInteractable(selectedTotal > 0);
    }

    public void SelectStatUpgrade(int statIndex)
    {
        if (statIndex < 0 || statIndex >= pendingUpgradeCounts.Length)
        {
            return;
        }

        if (GetPendingUpgradeTotal() >= CurrentAvailablePoints)
        {
            statusMessage = "사용 가능한 포인트가 없습니다.";
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
            statusMessage = "선택한 스탯이 없습니다.";
            Refresh();
            return;
        }

        BindRuntimeReferences();

        bool appliedToBackend = false;
        isConfirming = true;

        try
        {
            for (int i = 0; i < StatBindings.Length; i++)
            {
                int count = pendingUpgradeCounts[i];
                if (count <= 0)
                {
                    continue;
                }

                StatBinding binding = StatBindings[i];

                if (playerStatManager != null)
                {
                    appliedToBackend |= playerStatManager.ChangeInvestment(binding.StatType, count) > 0;
                }

                onStatUpgradeConfirmed.Invoke(binding.StatType.ToString(), count);
                StatUpgradeConfirmed?.Invoke(binding.StatType.ToString(), count);
            }

            if (playerStatManager != null)
            {
                playerStatManager.ConfirmInvestment();
            }
            else
            {
                availablePoints = Mathf.Max(0, availablePoints - selectedTotal);
            }
        }
        finally
        {
            isConfirming = false;
        }

        Array.Clear(pendingUpgradeCounts, 0, pendingUpgradeCounts.Length);
        statusMessage = appliedToBackend
            ? "강화 적용 완료"
            : "선택 내역 확정 완료";
        Refresh();
    }

    public void Upgrade()
    {
        Confirm();
    }

    private void RefreshRow(int index)
    {
        if (index < 0 || index >= rows.Length || rows[index] == null)
        {
            return;
        }

        StatBinding binding = StatBindings[index];
        StatRowView row = rows[index];
        ResolveStatValues(binding, out float currentValue, out float stepValue);

        int pendingCount = pendingUpgradeCounts[index];
        int previewCount = Mathf.Max(1, pendingCount);
        float previewValue = currentValue + stepValue * previewCount;

        SetText(row.NameText, binding.Label);

        string currentText = FormatValue(currentValue, binding.Suffix);
        string valueText = pendingCount > 0
            ? $"{currentText}  <size=85%>►</size>  <color=#d7ff32>{FormatValue(previewValue, binding.Suffix)}</color>"
            : currentText;
        SetText(row.ValueText, valueText);

        if (row.PlusButton != null)
        {
            row.PlusButton.interactable = playerStatManager != null || useFallbackPreviewValues;
        }
    }

    private void ResolveStatValues(StatBinding binding, out float currentValue, out float stepValue)
    {
        if (playerStatManager != null)
        {
            currentValue = GetCurrentValue(binding.StatType);
            stepValue = playerStatManager.GetAmountPerPoint(binding.StatType);
            return;
        }

        if (useFallbackPreviewValues)
        {
            currentValue = binding.FallbackCurrentValue;
            stepValue = binding.FallbackStepValue;
            return;
        }

        currentValue = 0f;
        stepValue = 0f;
    }

    private float GetCurrentValue(PlayerStatType statType)
    {
        return statType switch
        {
            PlayerStatType.Health => playerStatManager.Health,
            PlayerStatType.MeleeDamage => playerStatManager.MeleeDamage,
            PlayerStatType.RangedDamage => playerStatManager.RangedDamage,
            PlayerStatType.CriticalProbability => playerStatManager.CriticalProbability,
            PlayerStatType.MoveSpeed => playerStatManager.MoveSpeed,
            PlayerStatType.AttackSpeed => playerStatManager.AttackSpeed,
            PlayerStatType.SkillArea => playerStatManager.SkillArea,
            _ => 0f,
        };
    }

    private string BuildFeedbackText()
    {
        if (!string.IsNullOrEmpty(statusMessage))
        {
            return statusMessage;
        }

        if (playerStatManager == null && useFallbackPreviewValues)
        {
            return "PlayerStatManager를 찾지 못했습니다. 미리보기 값으로 표시합니다.";
        }

        return string.Empty;
    }

    private void BindRuntimeReferences()
    {
        if (playerStatManager == null && autoFindPlayerStatManager && Time.unscaledTime >= nextRuntimeSearchTime)
        {
            nextRuntimeSearchTime = Time.unscaledTime + 2f;
            playerStatManager = FindAnyObjectByType<PlayerStatManager>();
        }

        Subscribe();
    }

    private void Subscribe()
    {
        if (subscribedManager == playerStatManager)
        {
            return;
        }

        Unsubscribe();
        subscribedManager = playerStatManager;

        if (subscribedManager != null)
        {
            subscribedManager.OnStatsChanged += HandleStatsChanged;
            subscribedManager.OnPointsChanged += HandlePointsChanged;
        }
    }

    private void Unsubscribe()
    {
        if (subscribedManager == null)
        {
            return;
        }

        subscribedManager.OnStatsChanged -= HandleStatsChanged;
        subscribedManager.OnPointsChanged -= HandlePointsChanged;
        subscribedManager = null;
    }

    private void HandleStatsChanged()
    {
        if (!isConfirming)
        {
            Refresh();
        }
    }

    private void HandlePointsChanged(int remainingPoints)
    {
        if (!isConfirming)
        {
            Refresh();
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

    private void BringToFront()
    {
        transform.SetAsLastSibling();
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

    [Serializable]
    public sealed class StatUpgradeConfirmedEvent : UnityEvent<string, int>
    {
    }

    [Serializable]
    private sealed class StatRowView
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Button plusButton;

        public TMP_Text NameText => nameText;
        public TMP_Text ValueText => valueText;
        public Button PlusButton => plusButton;
    }

    private readonly struct StatBinding
    {
        public StatBinding(
            string label,
            PlayerStatType statType,
            string suffix,
            float fallbackCurrentValue,
            float fallbackStepValue)
        {
            Label = label;
            StatType = statType;
            Suffix = suffix;
            FallbackCurrentValue = fallbackCurrentValue;
            FallbackStepValue = fallbackStepValue;
        }

        public string Label { get; }
        public PlayerStatType StatType { get; }
        public string Suffix { get; }
        public float FallbackCurrentValue { get; }
        public float FallbackStepValue { get; }
    }
}
