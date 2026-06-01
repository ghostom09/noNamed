using UnityEngine;
using System.Collections;

public class SoldierContinueAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly GameObject _bulletPrefab;
    private readonly MonoBehaviour _coroutineRunner;
 
    public SoldierContinueAttack(Enemy enemy, GameObject bulletPrefab)
    {
        _enemy = enemy;
        _bulletPrefab = bulletPrefab;
        _coroutineRunner = enemy;
    }
 
    public void Attack()
    {
        if (!_enemy.CanAttackSpeed()) return;
 
        _enemy.ResetAttackTimer();
        
        _coroutineRunner.StartCoroutine(Spread());
    }

    private IEnumerator Spread()
    {
        for (int i = 0; i < 10; i++)
        {
            float angle = Mathf.Atan2(_enemy.GetVector2().y, _enemy.GetVector2().x) * Mathf.Rad2Deg;
            var bullet =Object.Instantiate
                (_bulletPrefab, _enemy.transform.position, Quaternion.Euler(0, 0, angle));
            bullet.GetComponent<BulletBase>()?.Init(_enemy.stats.damage, _enemy.GetVector2());

            yield return new WaitForSeconds(0.2f);   
        }
    }
}