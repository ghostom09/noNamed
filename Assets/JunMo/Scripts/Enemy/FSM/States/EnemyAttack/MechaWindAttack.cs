using UnityEngine;
using System.Collections;

public class MechaWindAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly MonoBehaviour _coroutineRunner;
 
    private const float SwingAngle = 120f;  // 부채꼴 각도
    private const float SwingRange = 2f;    // 공격 범위
    private const int RayCount = 10;        // 부채꼴 판정 정밀도
 
    public MechaWindAttack(Enemy enemy)
    {
        _enemy = enemy;
        _coroutineRunner = enemy;
    }
 
    public void Attack()
    {
        if(_enemy.IsAttacking) return;
        
        _coroutineRunner.StartCoroutine(SwingAttack());
    }
 
    private IEnumerator SwingAttack()
    {
        _enemy.rb.linearVelocity = Vector3.zero;
        _enemy.IsAttacking = true;
        _enemy.AttackWarn();

        yield return new WaitForSeconds(_enemy.stats.durationWarning);
        if (!_enemy)
            yield break;

        float angle2 = Mathf.Atan2(_enemy.GetVector2().y, _enemy.GetVector2().x) * Mathf.Rad2Deg;
        var prefab = Object.Instantiate
            (EnemyPrefabController.Instance.GetPrefab(_enemy.stats.attackType),
                _enemy.transform.position, Quaternion.Euler(0, 0, angle2 - 110f));
        Object.Destroy(prefab, 0.5f);
        
        yield return new WaitForSeconds(0.2f);
    
        Vector2 baseDir = _enemy.GetVector2();
    
        Collider2D[] targets =
            Physics2D.OverlapCircleAll(
                _enemy.transform.position,
                SwingRange
            );
    
        foreach (Collider2D target in targets)
        {
            if (!target.CompareTag("Player"))
                continue;
    
            Vector2 dirToTarget =
                (target.transform.position - _enemy.transform.position).normalized;
    
            float angle =
                Vector2.Angle(baseDir, dirToTarget);
    
            if (angle <= SwingAngle * 0.5f)
            {
                Debug.Log("[Meka2] 부채꼴 공격 히트");
                break;
            }
        }
        _enemy.IsAttacking = false;
        _enemy.ResetAttackTimer();
    }
}