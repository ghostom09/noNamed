using System.Collections.Generic;
using Global;
using UnityEngine;

public class RoomManager : Singleton<RoomManager>
{

    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private float roomSpacing = 10f;
    
    private List<RoomController> _roomControllers = new List<RoomController>();
    private RoomNode _currentRoom;
    
}
