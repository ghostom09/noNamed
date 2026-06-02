using System.Collections.Generic;
using Global;
using UnityEngine;

public class RoomManager : Singleton<RoomManager>
{
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private Transform roomRoot;
    [SerializeField] private bool scaleFallbackPrefabToRoomSize = true;

    private readonly List<RoomController> _roomControllers = new List<RoomController>();
    private readonly List<GameObject> _spawnedRooms = new List<GameObject>();
    private RoomController _currentRoom;

    public void SpawnAllRooms(List<RoomNode> rooms)
    {
        ClearRooms();

        if (rooms == null)
        {
            Debug.LogError("Room list is null.");
            return;
        }

        for (var i = 0; i < rooms.Count; i++)
        {
            SpawnRoom(rooms[i]);
        }
    }

    private void SpawnRoom(RoomNode data)
    {
        var prefab = data.Prefab != null ? data.Prefab : roomPrefab;
        if (prefab == null)
        {
            Debug.LogError($"No prefab assigned for room {data.Id} ({data.Type}).");
            return;
        }

        var worldPos = new Vector3(data.Position.x, data.Position.y, 0f);
        var obj = Instantiate(prefab, worldPos, Quaternion.identity, roomRoot);

        if (data.Prefab == null && scaleFallbackPrefabToRoomSize)
            obj.transform.localScale = new Vector3(data.Size.x, data.Size.y, 1f);
        // if (scaleFallbackPrefabToRoomSize)
        //     obj.transform.localScale = new Vector3(data.Size.x, data.Size.y, 1f);

        var controller = obj.GetComponent<RoomController>();
        if (controller == null)
            controller = obj.GetComponentInChildren<RoomController>();

        if (controller == null)
        {
            Debug.LogError($"Room prefab does not have a RoomController: {data.Id} ({data.Type}).");
            Destroy(obj);
            return;
        }

        controller.Init(data);
        _roomControllers.Add(controller);
        _spawnedRooms.Add(obj);
    }

    private void ClearRooms()
    {
        for (var i = 0; i < _spawnedRooms.Count; i++)
        {
            if (_spawnedRooms[i] != null)
                Destroy(_spawnedRooms[i]);
        }

        _spawnedRooms.Clear();
        _roomControllers.Clear();
        _currentRoom = null;
    }

    public void EnterRoom(RoomController room)
    {
        if (_currentRoom == room)
            return;

        _currentRoom = room;
        room.OnEnter();

        Debug.Log($"Entered room: {room.RoomData.Type} / ID: {room.RoomData.Id}");
    }

    public void OnRoomCleared(RoomController room)
    {
        room.OnCleared();
        Debug.Log($"Cleared room: {room.RoomData.Type} / ID: {room.RoomData.Id}");
    }

    public RoomController GetRoom(int roomId)
    {
        return _roomControllers.Find(r => r != null && r.RoomData != null && r.RoomData.Id == roomId);
    }

    public RoomController GetCurrentRoom()
    {
        return _currentRoom;
    }
}
