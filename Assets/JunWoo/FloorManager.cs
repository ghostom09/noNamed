using Global;
using UnityEngine;

public class FloorManager : Singleton<FloorManager>
{
    private const int MaxFloor = 12;
    private int _currentFloor = 1;

    [SerializeField] private DungeonGenerator dungeonGenerator;
    [SerializeField] private RoomManager roomManager;

    private bool _isBossCleared;

    private void Start()
    {
        LoadFloor(_currentFloor);
    }

    private void LoadFloor(int floor)
    {
        _isBossCleared = false;

        var rooms = dungeonGenerator.GenerateRooms(floor);
        roomManager.SpawnAllRooms(rooms);

        Debug.Log($"{floor}층 로드 완료");
    }
    
    public void OnBossCleared()
    {
        _isBossCleared = true;
        Debug.Log("보스 처치 완료");

        // TODO : 보스 클리어 연출 요청
    }
    
    public void TryAdvanceFloor()
    {
        if (_currentFloor % 3 == 0 && !_isBossCleared)
        {
            Debug.Log("보스를 처치해야 다음 층으로 이동 가능");
            // TODO : 경고 UI 요청
            return;
        }

        if (_currentFloor >= MaxFloor)
        {
            TriggerEnding();
            return;
        }

        _currentFloor++;
        LoadFloor(_currentFloor);

        Debug.Log($"{_currentFloor}층으로 이동");
    }

    private void TriggerEnding()
    {
        Debug.Log("엔딩 : 다시 잡히는 엔딩");
        // TODO : 크아아ㅏ아아ㅏ악
    }

    public int GetCurrentFloor() => _currentFloor;
}