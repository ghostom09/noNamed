using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatManager : MonoBehaviour
{
    [SerializeField] private PlayerStat playerStat;
    [SerializeField] private int pointsPerLevel = 3;
    [SerializeField] private int bonusPointsPerLevel = 2;
    [SerializeField] private int pointBonusMultiple = 5;
    public int Level           { get; private set; } = 1;
    public int AvailablePoints { get; private set; } = 0;

    public float Health              { get; private set; }
    public float MeleeDamage         { get; private set; }
    public float RangedDamage        { get; private set; }
    public float CriticalProbability { get; private set; }
    public float MoveSpeed           { get; private set; }
    public float AttackSpeed         { get; private set; }
    public float SkillArea           { get; private set; }

    public event Action<int> OnPointsChanged;
    public event Action      OnStatsChanged;

    private readonly int[] investedPoints = new int[System.Enum.GetValues(typeof(PlayerStatType)).Length];

    private void Awake()
    {
        ApplyStats();
    }

    public void LevelUp()
    {
        Level++;
        AvailablePoints += pointsPerLevel;
        if(Level%pointsPerLevel == 0) AvailablePoints += bonusPointsPerLevel;
        ApplyStats();
        OnPointsChanged?.Invoke(AvailablePoints);
        OnStatsChanged?.Invoke();
        Debug.Log($"Level Up → {Level}, Points: {AvailablePoints}");
    }

    public int ChangeInvestment(PlayerStatType statType, int delta)
    {
        int idx     = (int)statType;
        int current = investedPoints[idx];
        int next    = Mathf.Max(0, current + delta);

        int actualDelta = next - current;
        if (actualDelta > AvailablePoints)
        {
            actualDelta = AvailablePoints;
            next = current + actualDelta;
        }

        if (actualDelta == 0) return 0;

        investedPoints[idx]  = next;
        AvailablePoints     -= actualDelta;
        ApplyStats();
        OnPointsChanged?.Invoke(AvailablePoints);
        OnStatsChanged?.Invoke();
        return actualDelta;
    }

    public void ResetInvestment()
    {
        int refund = 0;
        for (int i = 0; i < investedPoints.Length; i++)
            refund += investedPoints[i];

        System.Array.Clear(investedPoints, 0, investedPoints.Length);
        AvailablePoints += refund;
        ApplyStats();
        OnPointsChanged?.Invoke(AvailablePoints);
        OnStatsChanged?.Invoke();
    }

    public void ConfirmInvestment()
    {
        OnStatsChanged?.Invoke();
    }

    public int GetInvestedPoints(PlayerStatType statType) => investedPoints[(int)statType];

    public float GetAmountPerPoint(PlayerStatType statType)
    {
        if (playerStat == null) return 0f;
        return playerStat.GetAmountPerPoint(statType);
    }

    private void ApplyStats()
    {
        if (playerStat == null)
        {
            Debug.LogWarning("PlayerStat is missing.");
            return;
        }

        Health              = playerStat.health              + investedPoints[(int)PlayerStatType.Health]              * playerStat.healthPerPoint;
        MeleeDamage         = playerStat.meleeDamage         + investedPoints[(int)PlayerStatType.MeleeDamage]         * playerStat.meleeDamagePerPoint;
        RangedDamage        = playerStat.rangedDamage        + investedPoints[(int)PlayerStatType.RangedDamage]        * playerStat.rangedDamagePerPoint;
        CriticalProbability = playerStat.criticalProbability + investedPoints[(int)PlayerStatType.CriticalProbability] * playerStat.criticalPerPoint;
        MoveSpeed           = playerStat.moveSpeed           + investedPoints[(int)PlayerStatType.MoveSpeed]           * playerStat.moveSpeedPerPoint;
        AttackSpeed         = playerStat.attackSpeed         + investedPoints[(int)PlayerStatType.AttackSpeed]         * playerStat.attackSpeedPerPoint;
        SkillArea           = playerStat.skillArea           + investedPoints[(int)PlayerStatType.SkillArea]           * playerStat.skillAreaPerPoint;
    }
}