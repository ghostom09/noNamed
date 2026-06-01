using UnityEngine;
using System.Collections;

public class SoldierFiveAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly GameObject _bulletPrefab;
 
    private const int BulletCount = 5;
    private const float SpreadAngle = 60f; // 총 퍼지는 각도
 
    public SoldierFiveAttack(Enemy enemy, GameObject bulletPrefab)
    {
        _enemy = enemy;
        _bulletPrefab = bulletPrefab;
        // stats.attackSpeed = 4f 로 설정
    }
 
    public void Attack()
    {
        if (!_enemy.CanAttackSpeed()) return;
 
        _enemy.ResetAttackTimer();
 
        Vector2 baseDir = _enemy.GetVector2();
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
 
        for (int i = 0; i < BulletCount; i++)
        {
            float angle = baseAngle - SpreadAngle / 2f + (SpreadAngle / (BulletCount - 1)) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            
            float angle2 = Mathf.Atan2(_enemy.GetVector2().y, _enemy.GetVector2().x) * Mathf.Rad2Deg;
            var bullet =Object.Instantiate
                (_bulletPrefab, _enemy.transform.position, Quaternion.Euler(0, 0, angle2));
            bullet.GetComponent<BulletBase>()?.Init(_enemy.stats.damage, dir);
        }
    }
}