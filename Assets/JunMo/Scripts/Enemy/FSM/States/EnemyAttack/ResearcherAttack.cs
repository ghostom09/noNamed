using System.Collections;
using UnityEngine;

public class ResearcherAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly GameObject _bulletPrefab; // Bullet_Normal
 
    public ResearcherAttack(Enemy enemy, GameObject bulletPrefab)
    {
        _enemy = enemy;
        _bulletPrefab = bulletPrefab;
    }
 
    public void Attack()
    {
        if (_enemy.IsAttacking) return;
        _enemy.StartCoroutine(Attacking());
    }

    private IEnumerator Attacking()
    {
        _enemy.IsAttacking = true;
        _enemy.AttackWarn();
        yield return new WaitForSeconds(_enemy.stats.durationWarning);
        
        if (!_enemy)
            yield break;

        _enemy.Animation?.Play(EnemyAnimationType.Attack);
        
        float angle = Mathf.Atan2(_enemy.GetVector2().y, _enemy.GetVector2().x) * Mathf.Rad2Deg;
        var bullet =Object.Instantiate
            (_bulletPrefab, _enemy.transform.position, Quaternion.Euler(0, 0, angle));
        bullet.GetComponent<BulletBase>()?.Init(_enemy.stats.damage, _enemy.GetVector2());
        _enemy.ResetAttackTimer();
        _enemy.IsAttacking = false;
    }
}
