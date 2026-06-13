using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statNameText;
    [SerializeField] private TextMeshProUGUI currentValueText;
    [SerializeField] private TextMeshProUGUI investedPointText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;

    private PlayerStatType statType;
    private PlayerStatManager statManager;
    private Action onChanged;

    public void Init(PlayerStatType type, PlayerStatManager manager, Action onChangedCallback)
    {
        statType    = type;
        statManager = manager;
        onChanged   = onChangedCallback;

        if (statNameText != null)
            statNameText.text = GetStatDisplayName(statType);

        if (minusButton != null)
            minusButton.onClick.AddListener(OnMinus);

        if (plusButton != null)
            plusButton.onClick.AddListener(OnPlus);

        Refresh();
    }

    private void OnDestroy()
    {
        if (minusButton != null) minusButton.onClick.RemoveListener(OnMinus);
        if (plusButton  != null) plusButton.onClick.RemoveListener(OnPlus);
    }

    private void OnPlus()
    {
        statManager.ChangeInvestment(statType, +1);
        onChanged?.Invoke();
    }

    private void OnMinus()
    {
        statManager.ChangeInvestment(statType, -1);
        onChanged?.Invoke();
    }

    public void Refresh()
    {
        if (statManager == null) return;

        float current  = GetCurrentStat();
        int   invested = statManager.GetInvestedPoints(statType);

        if (currentValueText != null)
        {
            currentValueText.text = FormatStat(current);
        }

        if (investedPointText != null)
            investedPointText.text = invested.ToString();

        if (minusButton != null)
            minusButton.interactable = invested > 0;

        if (plusButton != null)
            plusButton.interactable = statManager.AvailablePoints > 0;
    }

    private float GetCurrentStat()
    {
        return statType switch
        {
            PlayerStatType.Health              => statManager.Health,
            PlayerStatType.MeleeDamage         => statManager.MeleeDamage,
            PlayerStatType.RangedDamage        => statManager.RangedDamage,
            PlayerStatType.CriticalProbability => statManager.CriticalProbability,
            PlayerStatType.MoveSpeed           => statManager.MoveSpeed,
            PlayerStatType.AttackSpeed         => statManager.AttackSpeed,
            PlayerStatType.SkillArea           => statManager.SkillArea,
            _                                  => 0f
        };
    }

    private static string FormatStat(float value)
    {
        return Mathf.Approximately(value % 1f, 0f) ? $"{(int)value}" : $"{value:0.#}";
    }

    private static string GetStatDisplayName(PlayerStatType statType)
    {
        return statType switch
        {
            PlayerStatType.Health              => "Health",
            PlayerStatType.MeleeDamage         => "Melee Damage",
            PlayerStatType.RangedDamage        => "Ranged Damage",
            PlayerStatType.CriticalProbability => "Critical",
            PlayerStatType.MoveSpeed           => "Move Speed",
            PlayerStatType.AttackSpeed         => "Attack Speed",
            PlayerStatType.SkillArea           => "Skill Area",
            _                                  => statType.ToString()
        };
    }
}