using System.Collections;
using UnityEngine;

public class MechaDashAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly MonoBehaviour _coroutineRunner;
 
    private const float DashSpeed = 15f;
    private const float DashDuration = 0.3f;
    private bool _isDashing;
 
    public MechaDashAttack(Enemy enemy)
    {
        _enemy = enemy;
        _coroutineRunner = enemy;
    }
 
    public void Attack()
    {
        if (!_enemy.CanAttackSpeed() || _isDashing) return;
 
        _enemy.ResetAttackTimer();
        _coroutineRunner.StartCoroutine(Dash());
    }
 
    private IEnumerator Dash()
    {
        _isDashing = true;
        Vector2 dashDir = _enemy.GetVector2();
        float elapsed = 0f;
 
        while (elapsed < DashDuration)
        {
            _enemy.rb.linearVelocity = dashDir * DashSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }
 
        _enemy.rb.linearVelocity = Vector2.zero;
        _isDashing = false;
    }
}