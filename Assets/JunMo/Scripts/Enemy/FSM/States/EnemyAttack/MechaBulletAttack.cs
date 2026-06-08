using UnityEngine;

public class MechaBulletAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly GameObject _homingBulletPrefab; // Bullet_Homing
    private readonly Transform _playerTransform;
 
    private const float HomingDuration = 3f;
 
    public MechaBulletAttack(Enemy enemy, GameObject homingBulletPrefab)
    {
        _enemy = enemy;
        _homingBulletPrefab = homingBulletPrefab;
    }
 
    public void Attack()
    {
        for (int i = 0; i < 2; i++)
        {
            float angle = Mathf.Atan2(_enemy.GetVector2().y, _enemy.GetVector2().x) * Mathf.Rad2Deg;
            var bullet =Object.Instantiate
                (_homingBulletPrefab, _enemy.transform.position, Quaternion.Euler(0, 0, angle));
            bullet.GetComponent<BulletBase>()?.Init(_enemy.stats.damage, _enemy.GetVector2());
        }
    }
}