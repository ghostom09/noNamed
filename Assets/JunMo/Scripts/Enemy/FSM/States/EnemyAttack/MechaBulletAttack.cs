using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MechaBulletAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly GameObject _homingBulletPrefab; // Bullet_Homing
    private readonly MonoBehaviour _coroutineRunner;
    private const float HomingDuration = 3f;
 
    public MechaBulletAttack(Enemy enemy, GameObject homingBulletPrefab)
    {
        _enemy = enemy;
        _homingBulletPrefab = homingBulletPrefab;
        _coroutineRunner = enemy;
    }
 
    public void Attack()
    {
        if (!_enemy.CanAttackSpeed()) return;
        if (_enemy.IsAttacking) return;
        _coroutineRunner.StartCoroutine(Shoot());
    }

    private IEnumerator Shoot()
    {
        _enemy.IsAttacking = true;
        _enemy.AttackWarn();
        yield return new WaitForSeconds(_enemy.stats.durationWarning);
        if (!_enemy)
            yield break;
        Transform player = _enemy.GetTransform();
        
        for (int i = 0; i < 2; i++)
        {
            float angle = Mathf.Atan2(_enemy.GetVector2().y, _enemy.GetVector2().x) * Mathf.Rad2Deg;

            var bullet = Object.Instantiate(
                _homingBulletPrefab,
                _enemy.transform.position,
                Quaternion.Euler(0, 0, angle)
            );

            bullet.GetComponent<HomingBullet>()?.InitHoming(
                _enemy.stats.damage,
                player,
                6f,
                HomingDuration
            );
            
            yield return new WaitForSeconds(0.5f);
        }
        _enemy.IsAttacking = false;
        _enemy.ResetAttackTimer();
    } 
}