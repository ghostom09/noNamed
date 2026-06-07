namespace JunWoo
{
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class EnemySpawnPattern
    {
        [SerializeField] private string patternName = "Default";
        [SerializeField, Min(1)] private int weight = 1;
        [SerializeField, Min(0)] private int minSpawnCount = 1;
        [SerializeField, Min(0)] private int maxSpawnCount = 3;
        [SerializeField] private MonsterSpawnData monsterSpawnData;
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        public string PatternName => patternName;
        public int Weight => Mathf.Max(1, weight);
        public MonsterSpawnData MonsterSpawnData => monsterSpawnData;
        public List<Transform> SpawnPoints => spawnPoints;

        public int GetSpawnCount()
        {
            int min = Mathf.Max(0, minSpawnCount);
            int max = Mathf.Max(min, maxSpawnCount);

            return Random.Range(min, max + 1);
        }

        public bool IsValid()
        {
            return monsterSpawnData != null &&
                   spawnPoints != null &&
                   spawnPoints.Count > 0;
        }
    }
}