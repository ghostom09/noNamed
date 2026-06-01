using System.Collections.Generic;
using UnityEngine;

public class MapSpawn : MonoBehaviour
{
    public List<Transform> spawnPoints;
    [SerializeField]private MonsterSpawnData monsterSpawnData;

    private void Start()
    {
        EnemySpawner.Instance.Spawn(spawnPoints, monsterSpawnData);
    }
}