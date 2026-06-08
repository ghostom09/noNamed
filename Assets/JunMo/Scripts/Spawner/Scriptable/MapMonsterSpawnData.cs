using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MonsterSpawnInfo
{
    public GameObject monsterPrefab;
    [Range(0, 100)]
    public int weight;
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/EnemySpawnData")]
public class MonsterSpawnData : ScriptableObject
{
    public List<MonsterSpawnInfo> normalMonsters;
    public List<MonsterSpawnInfo> eliteMonsters;
    public List<MonsterSpawnInfo> bossMonsters;

    public bool isNormal = true;
    public bool isElite = false;
    public bool isBoss = false;
}