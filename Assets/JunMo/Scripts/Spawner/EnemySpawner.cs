using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;
    private List<Enemy> _enemies = new();
    public void Spawn(List<Transform> spawnPoints, MonsterSpawnData monsterSpawnData)
    {
        int index = Random.Range(0, spawnPoints.Count);
        var spawnPoint = spawnPoints[index];
        spawnPoints.RemoveAt(index);

        GameObject monsterPrefab = GetRandomMonster(monsterSpawnData);
        _enemies.Add(monsterPrefab.GetComponent<Enemy>());

        Instantiate(
            monsterPrefab,
            spawnPoint.position,
            Quaternion.identity
        );
    }
    
    private GameObject GetRandomMonster(MonsterSpawnData data)
    {
        List<MonsterSpawnInfo> monsters;

        if (data.isBoss)
        {
            monsters = data.bossMonsters;
        }
        else if (data.isElite)
        {
            monsters = data.eliteMonsters;
        }
        else
        {
            monsters = data.normalMonsters;
        }

        return GetWeightedRandomMonster(monsters);
    }

    private GameObject GetWeightedRandomMonster(List<MonsterSpawnInfo> monsters)
    {
        if (monsters == null || monsters.Count == 0)
        {
            Debug.LogError("몬스터 리스트가 비어있음");
            return null;
        }

        int totalWeight = 0;

        foreach (var monster in monsters)
        {
            totalWeight += monster.weight;
        }

        int randomValue = Random.Range(0, totalWeight);

        foreach (var monster in monsters)
        {
            randomValue -= monster.weight;

            if (randomValue < 0)
            {
                return monster.monsterPrefab;
            }
        }

        return monsters[0].monsterPrefab;
    }
    
    public void OnMonsterDead(Enemy enemy)
    {
        _enemies.Remove(enemy);

        if (_enemies.Count == 0)
        {
            // 여는거
        }
    }
}
