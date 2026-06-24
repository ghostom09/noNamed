using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorController : MonoBehaviour
{
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
        _connection = connection;
        _direction = RoomDataUtility.NormalizeDirection(direction);

        gameObject.SetActive(connection != null);
    }
}