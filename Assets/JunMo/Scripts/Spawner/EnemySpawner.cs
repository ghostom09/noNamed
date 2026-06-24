using System;
using UnityEngine;
using System.Collections.Generic;
using Global;
using Random = UnityEngine.Random;

public class EnemySpawner : Singleton<EnemySpawner>
{
    private readonly List<Enemy> _enemies = new();
    private List<Transform> _spawnPoints = new();

    public void InitList(List<Transform> spawnPoints)
    {
        _spawnPoints = spawnPoints != null
            ? new List<Transform>(spawnPoints)
            : new List<Transform>();
    }

    public int SpawnWave(
        List<Transform> spawnPoints,
        MonsterSpawnData monsterSpawnData,
        int count,
        Action<Enemy> onEnemyDead)
    {
        _enemies.Clear();

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("Spawn point가 없습니다.");
            return 0;
        }

        if (monsterSpawnData == null)
        {
            Debug.LogWarning("MonsterSpawnData가 없습니다.");
            return 0;
        }

        _spawnPoints = new List<Transform>(spawnPoints);

        int spawnedCount = 0;
        int safeCount = Mathf.Max(0, count);

        for (int i = 0; i < safeCount; i++)
        {
            if (SpawnOne(monsterSpawnData, onEnemyDead))
                spawnedCount++;
        }

        return spawnedCount;
    }

    private bool SpawnOne(MonsterSpawnData monsterSpawnData, Action<Enemy> onEnemyDead)
    {
        Transform spawnPoint = RandomTransform();
        if (spawnPoint == null)
            return false;

        GameObject monsterPrefab = GetRandomMonster(monsterSpawnData);
        if (monsterPrefab == null)
            return false;

        GameObject enemyObj = Instantiate(monsterPrefab, spawnPoint.position, Quaternion.identity);
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null)
        {
            Debug.LogError($"{monsterPrefab.name} 프리팹에 Enemy 컴포넌트가 없습니다.");
            Destroy(enemyObj);
            return false;
        }

        _enemies.Add(enemy);

        enemy.OnDead += OnMonsterDead;

        if (onEnemyDead != null)
            enemy.OnDead += onEnemyDead;

        return true;
    }

    private Transform RandomTransform()
    {
        if (_spawnPoints.Count == 0)
        {
            Debug.LogWarning("사용 가능한 spawn point가 없습니다.");
            return null;
        }

        int index = Random.Range(0, _spawnPoints.Count);
        Transform spawnPoint = _spawnPoints[index];
        _spawnPoints.RemoveAt(index);

        return spawnPoint;
    }

    private GameObject GetRandomMonster(MonsterSpawnData data)
    {
        if (data == null)
        {
            Debug.LogError("MonsterSpawnData가 없습니다.");
            return null;
        }

        if (data.isBoss)
            return GetWeightedRandomMonster(data.bossMonsters);

        if (data.isElite)
            return GetWeightedRandomMonster(data.eliteMonsters);

        return GetWeightedRandomMonster(data.normalMonsters);
    }

    private GameObject GetWeightedRandomMonster(List<MonsterSpawnInfo> monsters)
    {
        if (monsters == null || monsters.Count == 0)
        {
            Debug.LogError("몬스터 리스트가 비어있습니다.");
            return null;
        }

        int totalWeight = 0;

        foreach (var monster in monsters)
        {
            if (monster == null || monster.monsterPrefab == null || monster.weight <= 0)
                continue;

            totalWeight += monster.weight;
        }

        if (totalWeight <= 0)
        {
            Debug.LogError("사용 가능한 몬스터 weight가 없습니다.");
            return null;
        }

        int randomValue = Random.Range(0, totalWeight);

        foreach (var monster in monsters)
        {
            if (monster == null || monster.monsterPrefab == null || monster.weight <= 0)
                continue;

            randomValue -= monster.weight;

            if (randomValue < 0)
                return monster.monsterPrefab;
        }

        return null;
    }

    private void OnMonsterDead(Enemy enemy)
    {
        if (enemy != null)
            enemy.OnDead -= OnMonsterDead;

        _enemies.Remove(enemy);
    }
}