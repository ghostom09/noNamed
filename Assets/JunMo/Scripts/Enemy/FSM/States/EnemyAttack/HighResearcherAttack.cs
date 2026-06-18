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
        if (!_enemy.CanAttackSpeed()) return;
        
        float angle = Mathf.Atan2(_enemy.GetVector2().y, _enemy.GetVector2().x) * Mathf.Rad2Deg;
        var bullet =Object.Instantiate
            (_bulletPrefab, _enemy.transform.position, Quaternion.Euler(0, 0, angle));
        bullet.GetComponent<SilenceBullet>()?.Init(_enemy.stats.damage, _enemy.GetVector2());
        _enemy.ResetAttackTimer();
    }
}