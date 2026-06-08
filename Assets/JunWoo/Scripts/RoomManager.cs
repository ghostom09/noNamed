using System.Collections.Generic;
using Global;
using UnityEngine;

public class RoomManager : Singleton<RoomManager>
{
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private Transform roomRoot;
    [SerializeField] private Transform doorRoot;
    [SerializeField] private Transform collisionRoot;
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

        var resolvedCollisionRoot = GetCollisionRoot();

        for (var i = 0; i < rooms.Count; i++)
        {
            SpawnRoom(rooms[i], resolvedCollisionRoot);
        }
    }

    private void SpawnRoom(RoomNode data, Transform resolvedCollisionRoot)
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

        controller.Init(data, doorRoot, resolvedCollisionRoot);
        _roomControllers.Add(controller);
        _spawnedRooms.Add(obj);
    }

    private Transform GetCollisionRoot()
    {
        if (collisionRoot != null)
            return collisionRoot;

        var root = new GameObject("GeneratedRoomCollisions");
        collisionRoot = root.transform;
        return collisionRoot;
    }

    private void ClearRooms()
    {
        for (var i = 0; i < _roomControllers.Count; i++)
        {
            if (_roomControllers[i] != null)
                _roomControllers[i].ClearGeneratedObjects();
        }

        for (var i = 0; i < _spawnedRooms.Count; i++)
        {
            if (_spawnedRooms[i] != null)
                Destroy(_spawnedRooms[i]);
        }

        _spawnedRooms.Clear();
        _roomControllers.Clear();
        _currentRoom = null;
    }

    public void MovePlayerToStartRoom(Transform player)
    {
        if (player == null || _roomControllers.Count == 0)
            return;

        var startRoom = _roomControllers[0];
        if (startRoom == null || startRoom.RoomData == null)
            return;

        MovePlayer(player, startRoom.RoomData.Position);
        EnterRoom(startRoom, player);
    }

    public void EnterRoom(RoomController room, Transform player = null)
    {
        if (_currentRoom == room)
            return;

        _currentRoom = room;
        room.OnEnter(player);

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

    private void MovePlayer(Transform player, Vector2 position)
    {
        player.position = new Vector3(position.x, position.y, player.position.z);

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}