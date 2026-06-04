using System.Collections;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    public float acceleration = 20f;
    public float deceleration = 25f;
    
    public float dashSpeed = 15f;
    public float dashTime = 0.3f;
    public float dashCoolTime = 1.5f;
    
    private bool _isDashing = false;
    private bool _canDash = true;
    public Vector2 moveDirection;
    private Rigidbody2D _rb;
    private PlayerStatManager _statManager;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _statManager = GetComponent<PlayerStatManager>();
        moveSpeed = _statManager.MoveSpeed;
    }
    
    private void FixedUpdate()
    {
        if (!_isDashing)
            Move();
    }
    
    private void Move()
    {
        Vector2 targetVelocity = moveDirection * moveSpeed;
        
        float currentAccelRate = (moveDirection.sqrMagnitude > 0.01f) ? acceleration : deceleration;
        
        float newX = Mathf.MoveTowards(_rb.linearVelocity.x, targetVelocity.x, currentAccelRate * Time.fixedDeltaTime);
        float newY = Mathf.MoveTowards(_rb.linearVelocity.y, targetVelocity.y, currentAccelRate * Time.fixedDeltaTime);
        
        _rb.linearVelocity = new Vector2(newX, newY);
    }

    public void Dash()
    {
        if (!_canDash || _isDashing) return;

        StartCoroutine(DashCoroutine());
    }

    private IEnumerator DashCoroutine()
    {
        _canDash = false;
        _isDashing = true;
        
        Vector2 dashDirection = moveDirection;
        _rb.linearVelocity = dashDirection * dashSpeed;
        
        yield return new WaitForSeconds(dashTime);
        _isDashing = false;
        _rb.linearVelocity = moveDirection * moveSpeed;

        yield return new WaitForSeconds(dashCoolTime - dashTime);
        
        _canDash = true;
    }
}