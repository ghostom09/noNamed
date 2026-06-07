using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorController : MonoBehaviour
{
    [SerializeField] private bool requireRoomCleared = true;

    private RoomController _owner;
    private RoomConnection _connection;
    private Collider2D _collider;
    private Vector2Int _direction = Vector2Int.right;

    public Vector2Int Direction => _direction;
    public bool HasConnection => _connection != null;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();

        if (_collider != null)
            _collider.isTrigger = true;
    }

    public void Init(RoomController owner, RoomConnection connection, Vector2Int direction)
    {
        _owner = owner;
        _connection = connection;
        _direction = RoomDataUtility.NormalizeDirection(direction);

        gameObject.SetActive(connection != null);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_owner == null || _connection == null)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (requireRoomCleared && !_owner.CanUseDoors)
            return;

        RoomManager.Instance.MoveThroughDoor(_connection, other.transform);
    }
}
