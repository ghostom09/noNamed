using System.Collections.Generic;
using JunWoo;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    public RoomNode RoomData { get; private set; }

    [Header("Combat")]
    [SerializeField] private List<Transform> exitPoints = new List<Transform>();
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private bool autoClearWhenNoEnemiesSpawned = true;

    [Header("Doors")]
    [SerializeField] private GameObject doorPrefab;

    [Header("Enemy Spawn")]
    [SerializeField] private List<EnemySpawnPattern> spawnPatterns = new List<EnemySpawnPattern>();

    private readonly List<GameObject> _blocks = new List<GameObject>();
    private readonly List<GameObject> _spawnedDoors = new List<GameObject>();
    private readonly List<DoorController> _doors = new List<DoorController>();

    private Transform _doorRoot;
    private int _remainingEnemies;
    private bool _isActive;

    public bool CanUseDoors => RoomData != null && (!RoomData.LocksDoors || RoomData.IsCleared);

    public void Init(RoomNode data)
    {
        Init(data, null);
    }

    public void Init(RoomNode data, Transform doorRoot)
    {
        RoomData = data;
        _doorRoot = doorRoot;
        _remainingEnemies = 0;
        _isActive = false;

        CreateConnectedDoors();
    }

    private void OnDestroy()
    {
        ClearDoors();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (RoomData == null) return;
        if (!other.CompareTag("Player")) return;

        RoomManager.Instance.EnterRoom(this);
    }

    public void OnEnter()
    {
        if (RoomData == null)
            return;

        if (RoomData.IsCleared)
        {
            _isActive = false;
            return;
        }

        if (_isActive)
            return;

        _isActive = true;

        if (RoomData.Type == RoomType.Exit)
        {
            _isActive = false;
            FloorManager.Instance.TryAdvanceFloor();
            return;
        }

        if (RoomData.StartsCombat)
        {
            StartCombat();
            return;
        }

        OnCleared();
    }

    private void StartCombat()
    {
        if (RoomData.LocksDoors)
            BlockExits();

        var pattern = GetRandomSpawnPattern();

        if (pattern == null)
        {
            Debug.LogWarning($"{RoomData.Type} 방에 사용 가능한 Spawn Pattern이 없습니다.");

            if (autoClearWhenNoEnemiesSpawned)
                OnCleared();

            return;
        }

        int spawnCount = pattern.GetSpawnCount();

        _remainingEnemies = EnemySpawner.Instance.SpawnWave(
            pattern.SpawnPoints,
            pattern.MonsterSpawnData,
            spawnCount,
            _ => OnEnemyDied()
        );

        if (_remainingEnemies <= 0 && autoClearWhenNoEnemiesSpawned)
            OnCleared();
    }

    private EnemySpawnPattern GetRandomSpawnPattern()
    {
        if (spawnPatterns == null || spawnPatterns.Count == 0)
            return null;

        var totalWeight = 0;

        for (var i = 0; i < spawnPatterns.Count; i++)
        {
            var pattern = spawnPatterns[i];

            if (pattern == null || !pattern.IsValid())
                continue;

            totalWeight += pattern.Weight;
        }

        if (totalWeight <= 0)
            return null;

        var randomValue = Random.Range(0, totalWeight);

        for (var i = 0; i < spawnPatterns.Count; i++)
        {
            var pattern = spawnPatterns[i];

            if (pattern == null || !pattern.IsValid())
                continue;

            randomValue -= pattern.Weight;

            if (randomValue < 0)
                return pattern;
        }

        return null;
    }

    public void OnEnemyDied()
    {
        if (!_isActive)
            return;

        _remainingEnemies--;
        if (_remainingEnemies <= 0)
            OnCleared();
    }

    public void OnCleared()
    {
        if (RoomData == null || RoomData.IsCleared)
            return;

        RoomData.IsCleared = true;
        _isActive = false;
        UnblockExits();
        HandleReward();
    }

    private void BlockExits()
    {
        if (blockPrefab == null || _blocks.Count > 0)
            return;

        if (exitPoints != null && exitPoints.Count > 0)
        {
            for (var i = 0; i < exitPoints.Count; i++)
            {
                var exitPoint = exitPoints[i];
                if (exitPoint == null)
                    continue;

                CreateExitBlock(exitPoint.position);
            }

            return;
        }

        for (var i = 0; i < _doors.Count; i++)
        {
            var door = _doors[i];
            if (door == null || !door.HasConnection)
                continue;

            CreateExitBlock(door.transform.position);
        }
    }

    private void UnblockExits()
    {
        for (var i = 0; i < _blocks.Count; i++)
        {
            if (_blocks[i] != null)
                Destroy(_blocks[i]);
        }

        _blocks.Clear();
    }

    private void CreateExitBlock(Vector3 position)
    {
        var parent = _doorRoot != null ? _doorRoot : transform;
        var block = Instantiate(blockPrefab, position, Quaternion.identity, parent);
        _blocks.Add(block);
    }

    private void HandleReward()
    {
        switch (RoomData.RewardType)
        {
            case RewardType.LabReward:
                // TODO: open lab reward UI.
                break;
            case RewardType.ArchiveReward:
                // TODO: open archive reward UI.
                break;
            case RewardType.RestReward:
                // TODO: ^^
                break;
            case RewardType.ContainmentReward:
                // TODO: ^^
                break;
            case RewardType.BossClear:
                FloorManager.Instance.OnBossCleared();
                break;
        }
    }

    private void CreateConnectedDoors()
    {
        ClearDoors();

        if (doorPrefab == null)
        {
            if (RoomData != null && RoomData.Connections != null && RoomData.Connections.Count > 0)
                Debug.LogWarning($"{name}에 Door Prefab이 연결되지 않았습니다.");

            return;
        }

        var anchors = GetComponentsInChildren<DoorAnchor>(true);
        for (var i = 0; i < anchors.Length; i++)
        {
            var anchor = anchors[i];
            if (anchor == null)
                continue;

            var connection = FindConnection(anchor.Direction);
            if (connection == null)
                continue;

            CreateDoor(anchor, connection);
        }
    }

    private void CreateDoor(DoorAnchor anchor, RoomConnection connection)
    {
        var parent = _doorRoot != null ? _doorRoot : null;
        var obj = Instantiate(doorPrefab, anchor.transform.position, anchor.transform.rotation, parent);

        var door = obj.GetComponent<DoorController>();
        if (door == null)
            door = obj.GetComponentInChildren<DoorController>(true);

        if (door == null)
            door = obj.AddComponent<DoorController>();

        var collider = door.GetComponent<Collider2D>();
        if (collider == null)
            collider = door.gameObject.AddComponent<BoxCollider2D>();

        collider.isTrigger = true;
        door.Init(this, connection, anchor.Direction);

        _spawnedDoors.Add(obj);
        _doors.Add(door);
    }

    private void ClearDoors()
    {
        for (var i = 0; i < _spawnedDoors.Count; i++)
        {
            if (_spawnedDoors[i] != null)
                Destroy(_spawnedDoors[i]);
        }

        _spawnedDoors.Clear();
        _doors.Clear();
    }

    public Vector2 GetDoorWorldPosition(Vector2Int direction)
    {
        var normalized = RoomDataUtility.NormalizeDirection(direction);

        for (var i = 0; i < _doors.Count; i++)
        {
            var door = _doors[i];
            if (door == null || !door.HasConnection)
                continue;

            if (RoomDataUtility.NormalizeDirection(door.Direction) == normalized)
                return door.transform.position;
        }

        if (RoomData != null)
            return RoomData.GetDoorWorldPosition(normalized);

        return transform.position;
    }

    private RoomConnection FindConnection(Vector2Int direction)
    {
        if (RoomData == null || RoomData.Connections == null)
            return null;

        var normalized = RoomDataUtility.NormalizeDirection(direction);

        for (var i = 0; i < RoomData.Connections.Count; i++)
        {
            var connection = RoomData.Connections[i];
            if (connection != null && RoomDataUtility.NormalizeDirection(connection.Direction) == normalized)
                return connection;
        }

        return null;
    }
}

