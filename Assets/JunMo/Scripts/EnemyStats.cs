using UnityEngine;

public enum EnemyType
{
    Melee,
    Ranged,
    BossMelee,
    BossRanged,
}

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Enemy/EnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("기본 스탯")]
    public float attackDamage;
    public float moveSpeed;
    public float attackSpeed;
    public float maxHealth;
    public float attackRange; //공격 state 범위
    public float chaseRange;
    
    [Header("몬스터 타입")]
    public EnemyType enemyType;
}
