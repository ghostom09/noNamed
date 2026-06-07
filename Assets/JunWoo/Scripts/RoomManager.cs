using System.Collections.Generic;
using Global;
using UnityEngine;

public class RoomManager : Singleton<RoomManager>
{
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private Transform roomRoot;
    [SerializeField] private Transform doorRoot;
    [SerializeField] private bool scaleFallbackPrefabToRoomSize = true;
    [SerializeField, Min(0f)] private float playerDoorSpawnOffset = 1.2f;

    private readonly List<RoomController> _roomControllers = new List<RoomController>();
    private readonly List<GameObject> _spawnedRooms = new List<GameObject>();
    private RoomController _currentRoom;
    private Transform _trackedPlayer;

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

        if (_trackedPlayer != null && rooms.Count > 0)
            MovePlayerToRoomCenter(rooms[0]);
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

        controller.Init(data, doorRoot);
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

    public void MoveThroughDoor(RoomConnection connection, Transform player)
    {
        if (connection == null || connection.To == null || player == null)
            return;

        var targetRoom = GetRoom(connection.To.Id);
        if (targetRoom == null)
        {
            Debug.LogWarning($"Target room not found: {connection.To.Id}");
            return;
        }

        _trackedPlayer = player;

        var moveDirection = RoomDataUtility.NormalizeDirection(connection.Direction);
        var targetDoorDirection = RoomDataUtility.GetOppositeDirection(moveDirection);
        var targetDoorPosition = targetRoom.GetDoorWorldPosition(targetDoorDirection);
        var spawnOffset = new Vector2(moveDirection.x, moveDirection.y) * playerDoorSpawnOffset;
        var spawnPosition = targetDoorPosition + spawnOffset;

        MovePlayer(player, spawnPosition);
        EnterRoom(targetRoom);
    }

    private void MovePlayerToRoomCenter(RoomNode room)
    {
        if (_trackedPlayer == null || room == null)
            return;

        MovePlayer(_trackedPlayer, room.Position);

        var startRoom = GetRoom(room.Id);
        if (startRoom != null)
            EnterRoom(startRoom);
    }

    private void MovePlayer(Transform player, Vector2 position)
    {
        player.position = new Vector3(position.x, position.y, player.position.z);

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}


