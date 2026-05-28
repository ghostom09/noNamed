using System.Collections.Generic;
using Global;
using UnityEngine;

public class RoomManager : Singleton<RoomManager>
{
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private float roomSpacing = 10f;

    private List<RoomController> _roomControllers = new List<RoomController>();
    private RoomController _currentRoom;
    

    public void SpawnAllRooms(List<RoomNode> rooms)
    {
        foreach (var controller in _roomControllers)
        {
            if (controller != null)
                Destroy(controller.gameObject);
        }

        _roomControllers.Clear();
        _currentRoom = null;

        foreach (var data in rooms)
        {
            var worldPos = new Vector3(
                data.GridPos.x * roomSpacing,
                data.GridPos.y * roomSpacing,
                0
            );

            var obj = Instantiate(roomPrefab, worldPos, Quaternion.identity);
            var controller = obj.GetComponent<RoomController>();

            if (controller == null)
            {
                Debug.LogError($"roomPrefab에 RoomController가 없음 : {data.Id}");
                continue;
            }

            controller.Init(data);
            _roomControllers.Add(controller);
        }
    }

    public void EnterRoom(RoomController room)
    {
        if (_currentRoom == room) return;

        _currentRoom = room;
        room.OnEnter();

        Debug.Log($"입장한 방 : {room.RoomData.Type} / ID : {room.RoomData.Id}");
    }

    public void OnRoomCleared(RoomController room)
    {
        room.OnCleared();

        Debug.Log($"클리어한 방 : {room.RoomData.Type} / ID : {room.RoomData.Id}");
    }
    
    public RoomController GetRoom(int roomId)
    {
        return _roomControllers.Find(r => r.RoomData.Id == roomId);
    }

    public RoomController GetCurrentRoom() => _currentRoom;
}