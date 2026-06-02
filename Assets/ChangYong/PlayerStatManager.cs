using UnityEngine;

public class PlayerStatManager : MonoBehaviour
{
    [SerializeField] private PlayerStat playerStat;

    public int Level { get; private set; } = 1;

    public float Health { get; private set; }
    public float MeleeDamage { get; private set; }
    public float RangedDamage { get; private set; }
    public float CriticalProbability { get; private set; }
    public float MoveSpeed { get; private set; }
    public float AttackSpeed { get; private set; }
    public float SkillArea { get; private set; }

    private void Awake()
    {
        ApplyLevelStat();
    }

    public void LevelUp()
    {
        Level++;
        ApplyLevelStat();
    }

    public void SetLevel(int level)
    {
        Level = Mathf.Max(1, level);
        ApplyLevelStat();
    }

    private void ApplyLevelStat()
    {
        if (playerStat == null)
        {
            Debug.LogWarning("PlayerStat is missing.");
            return;
        }

        PlayerStatBonus bonus = playerStat.GetLevelBonus(Level);

        Health              = playerStat.health              + bonus.health;
        MeleeDamage         = playerStat.meleeDamage         + bonus.meleeDamage;
        RangedDamage        = playerStat.rangedDamage        + bonus.rangedDamage;
        CriticalProbability = playerStat.criticalProbability + bonus.criticalProbability;
        MoveSpeed           = playerStat.moveSpeed           + bonus.moveSpeed;
        AttackSpeed         = playerStat.attackSpeed         + bonus.attackSpeed;
        SkillArea           = playerStat.skillArea           + bonus.skillArea;
    }
}