using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;
    private List<Enemy> _enemies = new();
    private List<Transform> _spawnPoints = new();
    
    public event Action on_Die;

    public void InitList(List<Transform> spawnPoints)
    {
        _spawnPoints = new List<Transform>(spawnPoints);
    }

    private Transform RandomTransform()
    {
        if (_spawnPoints.Count == 0)
        {
            Debug.LogWarning("사용 가능한 스폰 포인트가 없음");
            return null;
        }
        
        int index = Random.Range(0, _spawnPoints.Count);
        Transform spawnPoint = _spawnPoints[index];
        _spawnPoints.RemoveAt(index);
        return spawnPoint;
    }
    public void Spawn(MonsterSpawnData monsterSpawnData)
    {
        Transform spawnPoint = RandomTransform();

        GameObject monsterPrefab = GetRandomMonster(monsterSpawnData);

        GameObject enemyObj = Instantiate(
            monsterPrefab,
            spawnPoint.position,
            Quaternion.identity
        );
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        _enemies.Add(enemy);
        
        enemy.OnDead += OnMonsterDead;
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
        enemy.OnDead -= OnMonsterDead;
        _enemies.Remove(enemy);

        if (_enemies.Count == 0)
        {
            // 여는거
        }
    }
}
