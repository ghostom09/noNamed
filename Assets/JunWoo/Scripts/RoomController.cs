using System.Collections.Generic;
using JunWoo;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    public RoomNode RoomData { get; private set; }

    [Header("Room Area")]
    [SerializeField] private Collider2D roomEnterTrigger;
    [SerializeField] private bool fitRoomEnterTriggerToDefinition = true;
    [SerializeField, Min(0f)] private float roomEnterTriggerInset = 1f;

    [Header("Boundary")]
    [SerializeField] private RoomBoundaryBuilder boundaryBuilder;

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
    private Transform _collisionRoot;
    private int _remainingEnemies;
    private bool _isActive;

    public bool CanUseDoors => RoomData != null && (!RoomData.LocksDoors || RoomData.IsCleared);

    public void Init(RoomNode data)
    {
        Init(data, null, null);
    }

    public void Init(RoomNode data, Transform doorRoot)
    {
        Init(data, doorRoot, null);
    }

    public void Init(RoomNode data, Transform doorRoot, Transform collisionRoot)
    {
        RoomData = data;
        _doorRoot = doorRoot;
        _collisionRoot = collisionRoot;
        _remainingEnemies = 0;
        _isActive = false;

        SetupRoomEnterTrigger();
        BuildBoundary();
        CreateConnectedDoors();
    }

    private void OnDestroy()
    {
        ClearGeneratedObjects();
    }

    public void ClearGeneratedObjects()
    {
        UnblockExits();
        ClearDoors();

        if (boundaryBuilder != null)
            boundaryBuilder.Clear();
    }

    private void SetupRoomEnterTrigger()
    {
        if (!fitRoomEnterTriggerToDefinition || RoomData == null)
            return;

        if (roomEnterTrigger == null)
            roomEnterTrigger = GetComponent<Collider2D>();

        if (roomEnterTrigger == null)
            return;

        roomEnterTrigger.isTrigger = true;

        var box = roomEnterTrigger as BoxCollider2D;
        if (box == null)
            return;

        var scale = transform.lossyScale;
        var scaleX = Mathf.Approximately(scale.x, 0f) ? 1f : Mathf.Abs(scale.x);
        var scaleY = Mathf.Approximately(scale.y, 0f) ? 1f : Mathf.Abs(scale.y);
        var size = RoomData.Size;
        var inset = roomEnterTriggerInset * 2f;

        box.offset = Vector2.zero;
        box.size = new Vector2(
            Mathf.Max(0.1f, (size.x - inset) / scaleX),
            Mathf.Max(0.1f, (size.y - inset) / scaleY));
    }

    private void BuildBoundary()
    {
        if (RoomData == null)
            return;

        if (boundaryBuilder == null)
            boundaryBuilder = GetComponent<RoomBoundaryBuilder>();

        if (boundaryBuilder == null)
            boundaryBuilder = gameObject.AddComponent<RoomBoundaryBuilder>();

        boundaryBuilder.Build(RoomData, _collisionRoot);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (RoomData == null) return;
        if (!other.CompareTag("Player")) return;

        RoomManager.Instance.EnterRoom(this, other.transform);
    }

    public void OnEnter(Transform player)
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
            FloorManager.Instance.TryAdvanceFloor(player);
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
            Debug.LogWarning($"{RoomData.Type} room has no valid spawn pattern.");

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
        if (_blocks.Count > 0)
            return;

        if (boundaryBuilder != null && boundaryBuilder.HasDoorOpenings)
        {
            boundaryBuilder.BlockDoorOpenings(blockPrefab);
            return;
        }

        if (blockPrefab == null)
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
        if (boundaryBuilder != null)
            boundaryBuilder.ClearDoorLocks();

        for (var i = 0; i < _blocks.Count; i++)
        {
            if (_blocks[i] != null)
                Destroy(_blocks[i]);
        }

        _blocks.Clear();
    }

    private void CreateExitBlock(Vector3 position)
    {
        var parent = _collisionRoot != null ? _collisionRoot : (_doorRoot != null ? _doorRoot : transform);
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
                // TODO: open rest reward UI.
                break;
            case RewardType.ContainmentReward:
                // TODO: open containment reward UI.
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
                Debug.LogWarning($"{name} has connections, but no Door Prefab is assigned.");

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