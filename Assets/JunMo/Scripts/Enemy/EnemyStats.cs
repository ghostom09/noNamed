using UnityEngine;

public enum ChaseType
{
    MeleeMove,
    MeleeShort,
    Ranged,
}

public enum AttackType
{
    MechaDash,
    MechaBoom,
    MechaWind,
    MechaBullet,
    SoldierSpread,
    SoldierFive,
    SoldierThree,
    SoldierContinue,
    Researcher,
    HighResearcher
}

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Enemy/EnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("기본 스탯")]
    public float damage;
    public float moveSpeed;
    public float attackSpeed;
    public float maxHealth;
    public float attackRange; //공격 state 범위
    public float chaseRange; // 쫓아갈 거리
    public float kitingRange; // 카이팅
    
    [Header("몬스터 타입")]
    public ChaseType chaseType;
    public AttackType attackType;
}
