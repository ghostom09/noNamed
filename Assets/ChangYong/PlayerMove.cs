using System.Collections;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float acceleration = 25f;
    public float deceleration = 25f;
    
    [SerializeField] private float dashSpeed = 40f;
    [SerializeField] private float dashTime = 0.1f;
    [SerializeField] private float dashCoolTime = 1.5f;
    
    private bool _isDashing = false;
    private bool _canDash = true;
    private float _externalMoveSpeedMultiplier = 1f;
    public Vector2 moveDirection;
    private Rigidbody2D _rb;
    private PlayerStatManager _statManager;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _statManager = GetComponent<PlayerStatManager>();
    }
    
    private void FixedUpdate()
    {
        if (!_isDashing)
            Move();
    }
    
    private void Move()
    {
        Vector2 targetVelocity = moveDirection * _statManager.MoveSpeed * _externalMoveSpeedMultiplier;

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

    public void SetExternalMoveSpeedMultiplier(float multiplier)
    {
        _externalMoveSpeedMultiplier = Mathf.Clamp01(multiplier);
    }

    private IEnumerator DashCoroutine()
    {
        _canDash = false;
        _isDashing = true;
        
        Vector2 dashDirection = moveDirection;
        _rb.linearVelocity = dashDirection * dashSpeed;
        
        yield return new WaitForSeconds(dashTime);
        _isDashing = false;
        _rb.linearVelocity = moveDirection * _statManager.MoveSpeed * _externalMoveSpeedMultiplier;

        yield return new WaitForSeconds(dashCoolTime - dashTime);
        
        _canDash = true;
    }
}
