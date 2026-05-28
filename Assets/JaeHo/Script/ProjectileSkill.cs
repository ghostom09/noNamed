using UnityEngine;

/// <summary>
/// 투사체(Bullet)를 발사하는 스킬의 베이스 클래스.
/// SkillA(돌격소총), SkillB(저격총)가 상속.
/// </summary>
public abstract class ProjectileSkill : SkillBase
{
    [Header("--- Projectile Settings ---")]
    [SerializeField] private BulletData bulletData;

    protected override void Awake()
    {
        base.Awake();

        if (bulletData == null)
            Debug.LogError($"[{name}] BulletData가 할당되지 않음");
    }

    /// <summary>
    /// 지정 위치에서 방향으로 총알 발사.
    /// 태그는 현재 스킬의 CurrentTags를 자동으로 전달.
    /// </summary>
    protected void FireBullet(Vector2 origin, Vector2 direction)
    {
        Bullet bullet = BulletPool.Instance.Get(bulletData, origin, direction);
        bullet.Initialize(direction, attackDamage, CurrentTags, bulletData);
    }
}