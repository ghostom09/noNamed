using UnityEngine;
using System.Collections;

public class SoldierThreeAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly GameObject _bulletPrefab;
    private readonly MonoBehaviour _coroutineRunner;
 
    public SoldierThreeAttack(Enemy enemy, GameObject bulletPrefab)
    {
        _enemy = enemy;
        _bulletPrefab = bulletPrefab;
        _coroutineRunner = enemy;
    }
 
    public void Attack()
    {
        if (_enemy.IsAttacking) return;
 
        _coroutineRunner.StartCoroutine(BurstFire());
    }
 
    private IEnumerator BurstFire()
    {
        _enemy.IsAttacking = true;
        _enemy.AttackWarn();

        yield return new WaitForSeconds(_enemy.stats.durationWarning);

        if (!_enemy)
            yield break;
        for (int i = 0; i < 3; i++)
        {
            SpawnBullet();
            yield return new WaitForSeconds(0.3f);
        }
        _enemy.IsAttacking = false;
        _enemy.ResetAttackTimer();
    }
 
    private void SpawnBullet()
    {
        float angle = Mathf.Atan2(_enemy.GetVector2().y, _enemy.GetVector2().x) * Mathf.Rad2Deg;
        var bullet =Object.Instantiate
            (_bulletPrefab, _enemy.transform.position, Quaternion.Euler(0, 0, angle));
        bullet.GetComponent<BulletBase>()?.Init(_enemy.stats.damage, _enemy.GetVector2());bullet.GetComponent<BulletBase>()?.Init(_enemy.stats.damage, _enemy.GetVector2());
    }
}