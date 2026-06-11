using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    public RoomNode RoomData { get; private set; }

    [SerializeField] private List<Transform> exitPoints = new List<Transform>();
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private bool autoClearWhenNoEnemiesSpawned = true;

    private readonly List<GameObject> _blocks = new List<GameObject>();
    private int _remainingEnemies;  
    private bool _isActive;

    public void Init(RoomNode data)
    {
        RoomData = data;
        _remainingEnemies = 0;
        _isActive = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (RoomData == null) return;
        if (!other.CompareTag("Player")) return;
        if (RoomData.IsCleared) return;
        if (_isActive) return;

        RoomManager.Instance.EnterRoom(this);
    }

    public void OnEnter()
    {
        if (RoomData == null)
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

        // TODO: connect this to EnemySpawner and set _remainingEnemies from the spawn result.
        // _remainingEnemies = EnemySpawner.Instance.Spawn(RoomData);

        if (_remainingEnemies <= 0 && autoClearWhenNoEnemiesSpawned)
            OnCleared();
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

        for (var i = 0; i < exitPoints.Count; i++)
        {
            var exitPoint = exitPoints[i];
            if (exitPoint == null)
                continue;

            var block = Instantiate(blockPrefab, exitPoint.position, Quaternion.identity, transform);
            _blocks.Add(block);
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
}
