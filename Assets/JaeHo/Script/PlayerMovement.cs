using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    
    private Vector2 _movement;
    private Rigidbody2D _rb;

    private void Awake() => _rb = GetComponent<Rigidbody2D>();

    private void Update()
    {
        Move();
    }

    void OnMove(InputValue value)
    {
        _movement = value.Get<Vector2>();
    }

    void Move()
    {
        _rb.linearVelocity = new Vector2(_movement.x * speed, _movement.y * speed);
    }
}
