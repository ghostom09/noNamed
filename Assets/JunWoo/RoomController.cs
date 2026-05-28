using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    public RoomNode RoomData { get; private set; }

    [SerializeField] private List<Transform> exitPoints; // 출입구 위치들
    [SerializeField] private GameObject blockPrefab;     // 출입구 막는 오브젝트

    private List<GameObject> _blocks = new List<GameObject>();
    private int _remainingEnemies;
    private bool _isActive;

    public void Init(RoomNode data)
    {
        RoomData = data;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (RoomData.IsCleared) return;
        if (_isActive) return;

        RoomManager.Instance.EnterRoom(this);
    }

    public void OnEnter()
    {
        _isActive = true;

        switch (RoomData.Type)
        {
            case RoomType.Corridor:
                StartCombat();
                break;
            case RoomType.Lab:
                StartCombat();
                break;
            case RoomType.Archive:
                StartCombat();
                break;
            case RoomType.Boss:
                StartCombat();
                break;
            case RoomType.Passage:
                break;
            case RoomType.Exit:
                FloorManager.Instance.TryAdvanceFloor();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void StartCombat()
    {
        BlockExits();
        // TODO : 스폰요청
        // _remainingEnemies = EnemySpawner.Instance.Spawn(RoomData);
    }
    
    public void OnEnemyDied()
    {
        _remainingEnemies--;
        if (_remainingEnemies <= 0)
            OnCleared();
    }

    public void OnCleared()
    {
        RoomData.IsCleared = true;
        _isActive = false;
        UnblockExits();
        HandleReward();
    }

    private void BlockExits()
    {
        foreach (var block in exitPoints.Select(exit => Instantiate(blockPrefab, exit.position, Quaternion.identity, transform)))
        {
            _blocks.Add(block);
        }
    }

    private void UnblockExits()
    {
        foreach (var block in _blocks)
            Destroy(block);
        _blocks.Clear();
    }

    private void HandleReward()
    {
        switch (RoomData.Type)
        {
            case RoomType.Lab:
                // TODO : 스킬 선택 UI 요청
                break;
            case RoomType.Archive:
                // TODO : 실험일지 UI 요청
                break;
            case RoomType.Boss:
                FloorManager.Instance.OnBossCleared();
                // TODO : 보스 클리어 UI 요청
                break;
            case RoomType.Corridor:
                break;
            case RoomType.Passage:
                break;
            case RoomType.Exit:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}