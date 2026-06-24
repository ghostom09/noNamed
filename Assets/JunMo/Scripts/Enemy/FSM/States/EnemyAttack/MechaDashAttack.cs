using System.Collections;
using UnityEngine;

public class MechaDashAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly MonoBehaviour _coroutineRunner;
 
    private const float DashSpeed = 15f;
    private const float DashDuration = 0.3f;
    private bool _isDashing;
    private readonly MechaDashAttackHitBox _hitBox;
 
    public MechaDashAttack(Enemy enemy)
    {
        _enemy = enemy;
        _coroutineRunner = enemy;
        _hitBox = enemy.GetComponent<MechaDashAttackHitBox>()
                  ?? enemy.gameObject.AddComponent<MechaDashAttackHitBox>();
        _hitBox.Init(enemy);
    }
 
    public void Attack()
    {
        if (!_enemy.CanAttackSpeed() || _isDashing) return;
        
        _coroutineRunner.StartCoroutine(Dash());
    }
 
    private IEnumerator Dash()
    {
        _enemy.IsAttacking = true;
        Vector2 savedPlayerPosition = _enemy.GetPlayerVector2();
        _enemy.AttackWarn();
        yield return new WaitForSeconds(_enemy.stats.durationWarning);
        if (!_enemy)
            yield break;

        _isDashing = true;
        Vector2 toSavedPosition = savedPlayerPosition - (Vector2)_enemy.transform.position;
        Vector2 dashDir = toSavedPosition.sqrMagnitude > 0.0001f
            ? toSavedPosition.normalized
            : _enemy.GetVector2();

        _hitBox.SetDashing(true);
        float elapsed = 0f;
 
        while (elapsed < DashDuration)
        {
            _enemy.rb.linearVelocity = dashDir * DashSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }
 
        _enemy.ResetAttackTimer();
        _enemy.rb.linearVelocity = Vector2.zero;
        _hitBox.SetDashing(false);
        _isDashing = false;
        _enemy.IsAttacking = false;
    }
}
