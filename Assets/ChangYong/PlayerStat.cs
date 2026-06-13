using UnityEngine;

[CreateAssetMenu(menuName = "Stat/Player Stat")]
public class PlayerStat : ScriptableObject
{
    [Header("Base Stats")]
    public float health;
    public float meleeDamage;
    public float rangedDamage;
    public float criticalProbability;
    public float moveSpeed;
    public float attackSpeed;
    public float skillArea;

    [Header("Points Per Upgrade (1포인트 당 상승량)")]
    public float healthPerPoint        = 10f;
    public float meleeDamagePerPoint   = 2f;
    public float rangedDamagePerPoint  = 2f;
    public float criticalPerPoint      = 0.5f;
    public float moveSpeedPerPoint     = 0.1f;
    public float attackSpeedPerPoint   = 0.05f;
    public float skillAreaPerPoint     = 0.1f;

    public float GetAmountPerPoint(PlayerStatType statType)
    {
        return statType switch
        {
            PlayerStatType.Health              => healthPerPoint,
            PlayerStatType.MeleeDamage         => meleeDamagePerPoint,
            PlayerStatType.RangedDamage        => rangedDamagePerPoint,
            PlayerStatType.CriticalProbability => criticalPerPoint,
            PlayerStatType.MoveSpeed           => moveSpeedPerPoint,
            PlayerStatType.AttackSpeed         => attackSpeedPerPoint,
            PlayerStatType.SkillArea           => skillAreaPerPoint,
            _                                  => 0f
        };
    }
}

public enum PlayerStatType
{
    Health,
    MeleeDamage,
    RangedDamage,
    CriticalProbability,
    MoveSpeed,
    AttackSpeed,
    SkillArea
}