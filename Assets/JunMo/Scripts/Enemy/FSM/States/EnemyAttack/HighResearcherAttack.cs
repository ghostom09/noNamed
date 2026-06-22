using System.Collections;
using UnityEngine;

public class HighResearcherAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly GameObject _bulletPrefab; // Bullet_Silence
 
    public HighResearcherAttack(Enemy enemy, GameObject bulletPrefab)
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

        Vector2 dir = _enemy.GetVector2();

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        var bullet = Object.Instantiate(
            _bulletPrefab,
            _enemy.transform.position,
            Quaternion.Euler(0, 0, angle)
        );

        bullet.GetComponent<SilenceBullet>()?.Init(
            _enemy.stats.damage,
            dir,
            _enemy.stats.silenceDuration
        );

        _enemy.ResetAttackTimer();
        _enemy.IsAttacking = false;
    }
}
