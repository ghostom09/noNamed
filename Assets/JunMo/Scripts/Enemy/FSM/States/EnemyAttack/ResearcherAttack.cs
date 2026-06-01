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
        float angle = Mathf.Atan2(_enemy.GetVector2().y, _enemy.GetVector2().x) * Mathf.Rad2Deg;
        var bullet =Object.Instantiate
            (_bulletPrefab, _enemy.transform.position, Quaternion.Euler(0, 0, angle));
        bullet.GetComponent<BulletBase>()?.Init(_enemy.stats.damage, _enemy.GetVector2());
    }
}