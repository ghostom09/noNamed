using UnityEngine;
using System.Collections;

public class SoldierSpreadAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly GameObject _bulletPrefab;
    private readonly MonoBehaviour _coroutineRunner;

    private const float RandomAngleRange = 30f;
    private const float SpreadFireRate = 0.2f;
    private const float SpreadDuration = 2f;
    private const float Cooldown = 5f;

    private float _timer;

    public SoldierSpreadAttack(Enemy enemy, GameObject bulletPrefab)
    {
        _enemy = enemy;
        _bulletPrefab = bulletPrefab;
        _coroutineRunner = enemy;

        _timer = Cooldown;
    }

    public void Attack()
    {
        _timer += Time.deltaTime;

        if (_timer < Cooldown)
            return;

        _timer = 0f;

        // 공격 시작 시 방향 고정
        Vector2 fixedDir = _enemy.GetVector2();

        _coroutineRunner.StartCoroutine(SpreadFire(fixedDir));
    }

    private IEnumerator SpreadFire(Vector2 fixedDir)
    {
        float elapsed = 0f;

        while (elapsed < SpreadDuration)
        {
            FireBullet(fixedDir);

            yield return new WaitForSeconds(SpreadFireRate);

            elapsed += SpreadFireRate;
        }
    }

    private void FireBullet(Vector2 fixedDir)
    {
        float baseAngle =
            Mathf.Atan2(fixedDir.y, fixedDir.x) * Mathf.Rad2Deg;

        float randomAngle =
            Random.Range(-RandomAngleRange, RandomAngleRange);

        float finalAngle = baseAngle + randomAngle;

        float rad = finalAngle * Mathf.Deg2Rad;

        Vector2 dir = new Vector2(
            Mathf.Cos(rad),
            Mathf.Sin(rad)
        );

        var bullet = Object.Instantiate(
            _bulletPrefab,
            _enemy.transform.position,
            Quaternion.Euler(0, 0, finalAngle)
        );

        bullet.GetComponent<BulletBase>()
            ?.Init(_enemy.stats.damage, dir);
    }
}