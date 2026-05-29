using System.Collections;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float dashSpeed = 15f;
    public float dashTime = 0.3f;
    public float dashCoolTime = 1.5f;
    
    private bool _isDashing = false;
    private bool _canDash = true;
    public Vector2 moveDirection;
    private Rigidbody2D _rb;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }
    
    private void FixedUpdate()
    {
        if (!_isDashing)
            Move();
    }
    
    private void Move()
    {
        _rb.linearVelocity = moveDirection * moveSpeed;
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

        yield return new WaitForSeconds(dashCoolTime - dashTime);
        
        _canDash = true;
    }
}