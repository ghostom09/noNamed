using UnityEngine;

[CreateAssetMenu(menuName = "Stat/Player Stat")]
public class PlayerStat : ScriptableObject
{
    public float health;
    public float meleeDamage;
    public float rangedDamage;
    public float criticalProbability;
    public float moveSpeed;
    public float attackSpeed;
    public float skillArea;

    public LevelRangeData[] levelTable;

    public PlayerStatBonus GetLevelBonus(int level)
    {
        PlayerStatBonus result = new();

        if (levelTable == null)
            return result;

        for (int i = 0; i < levelTable.Length; i++)
        {
            LevelRangeData data = levelTable[i];

            if (data == null || level < data.startLevel)
                continue;

            int endLevel = data.endLevel <= 0 ? data.startLevel : data.endLevel;
            int appliedEndLevel = Mathf.Min(level, endLevel);
            int appliedLevelCount = appliedEndLevel - data.startLevel + 1;

            if (appliedLevelCount <= 0)
                continue;

            result.health += data.health * appliedLevelCount;
            result.meleeDamage += data.meleeDamage * appliedLevelCount;
            result.rangedDamage += data.rangedDamage * appliedLevelCount;
            result.criticalProbability += data.criticalProbability * appliedLevelCount;
            result.moveSpeed += data.moveSpeed * appliedLevelCount;
            result.attackSpeed += data.attackSpeed * appliedLevelCount;
            result.skillArea += data.skillArea * appliedLevelCount;
        }

        return result;
    }
}

[System.Serializable]
public class LevelRangeData
{
    public int startLevel;
    public int endLevel;

    public float health;
    public float meleeDamage;
    public float rangedDamage;
    public float criticalProbability;
    public float moveSpeed;
    public float attackSpeed;
    public float skillArea;
}

public struct PlayerStatBonus
{
    public float health;
    public float meleeDamage;
    public float rangedDamage;
    public float criticalProbability;
    public float moveSpeed;
    public float attackSpeed;
    public float skillArea;
}