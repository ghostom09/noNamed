using System.Collections.Generic;
using UnityEngine;

public class MapSpawn : MonoBehaviour
{
    public List<Transform> spawnPoints;
    [SerializeField]private MonsterSpawnData monsterSpawnData;

    private void Awake()
    {
        EnemySpawner.Instance.InitList(spawnPoints);
    }

    private void Start()
    {
        EnemySpawner.Instance.Spawn(monsterSpawnData);
    }
}