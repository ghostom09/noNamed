using System.Collections.Generic;
using Global;
using UnityEngine;

public class FloorManager : Singleton<FloorManager>
{
    [SerializeField, Min(1)] private int maxFloor = 8;
    [SerializeField, Min(1)] private int startFloor = 1;
    [SerializeField] private DungeonGenerator dungeonGenerator;
    [SerializeField] private RoomManager roomManager;
    [SerializeField] private List<FloorRule> floorRules = new List<FloorRule>();

    private int _currentFloor;
    private bool _isBossCleared;

    private void Start()
    {
        _currentFloor = Mathf.Clamp(startFloor, 1, maxFloor);
        LoadFloor(_currentFloor);
    }

    private void LoadFloor(int floor, Transform playerToMove = null)
    {
        if (dungeonGenerator == null || roomManager == null)
        {
            Debug.LogError("FloorManager requires DungeonGenerator and RoomManager references.");
            return;
        }

        _isBossCleared = false;

        var rule = GetFloorRule(floor);
        var rooms = dungeonGenerator.GenerateRooms(floor, rule);
        roomManager.SpawnAllRooms(rooms);

        if (playerToMove != null)
            roomManager.MovePlayerToStartRoom(playerToMove);

        Debug.Log($"{floor} floor loaded.");
    }

    public void OnBossCleared()
    {
        _isBossCleared = true;
        Debug.Log("Boss cleared.");
    }

    public void TryAdvanceFloor(Transform playerToMove = null)
    {
        var rule = GetFloorRule(_currentFloor);
        if (RequiresBossClear(_currentFloor, rule) && !_isBossCleared)
        {
            Debug.Log("Boss must be cleared before advancing to the next floor.");
            return;
        }

        if (_currentFloor >= maxFloor)
        {
            TriggerEnding();
            return;
        }

        _currentFloor++;
        LoadFloor(_currentFloor, playerToMove);

        Debug.Log($"Moved to floor {_currentFloor}.");
    }

    private void TriggerEnding()
    {
        Debug.Log("Ending triggered.");
    }

    private FloorRule GetFloorRule(int floor)
    {
        for (var i = 0; i < floorRules.Count; i++)
        {
            if (floorRules[i] != null && floorRules[i].Floor == floor)
                return floorRules[i];
        }

        return null;
    }

    private bool RequiresBossClear(int floor, FloorRule rule)
    {
        if (rule != null)
            return rule.HasBoss;

        return dungeonGenerator != null && dungeonGenerator.IsFallbackBossFloor(floor);
    }

    public int GetCurrentFloor()
    {
        return _currentFloor;
    }
}

