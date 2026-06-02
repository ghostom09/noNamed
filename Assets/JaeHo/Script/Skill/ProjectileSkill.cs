using UnityEngine;

/// <summary>
/// 투사체(Bullet)를 발사하는 스킬의 베이스 클래스.
/// SkillA(돌격소총), SkillB(저격총)가 상속.
/// </summary>
public abstract class ProjectileSkill : SkillBase
{
    [Header("--- Projectile Settings ---")]
    [SerializeField] private BulletData bulletData;
    [Tooltip("산탄 변이 시 탄 사이 각도")]
    [SerializeField, Min(0f)] private float spreadAngleStep = 8f;

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
    protected void FireBullet(Vector2 origin, Vector2 direction, AttackShapeType shapeType = AttackShapeType.Bullet)
    {
        AttackContext context = CreateAttackContext(AttackRangeType.Ranged, shapeType, origin, direction);
        FireBulletPattern(context);
    }

    private void FireBulletPattern(AttackContext context)
    {
        if (bulletData == null) return;

        MutationGrade spreadGrade = MutationEffectResolver.GetGrade(
            context,
            MutationType.Spread,
            SkillTag.None,
            MutationTargetScope.Common);

        if (spreadGrade == MutationGrade.None)
        {
            SpawnBullet(context);
            return;
        }

        int pelletCount = ResolveSpreadCount(context, spreadGrade);
        float damageMultiplier = ResolveSpreadDamageMultiplier(context, spreadGrade);

        if (pelletCount <= 1)
        {
            SpawnBullet(context.WithDamageMultiplier(damageMultiplier));
            return;
        }

        float startOffset = -spreadAngleStep * (pelletCount - 1) * 0.5f;
        for (int i = 0; i < pelletCount; i++)
        {
            float angleOffset = startOffset + spreadAngleStep * i;
            Vector2 spreadDirection = Rotate(context.Direction, angleOffset);
            AttackContext pelletContext = context
                .WithOriginAndDirection(context.Origin, spreadDirection)
                .WithDamageMultiplier(damageMultiplier);

            SpawnBullet(pelletContext);
        }
    }

    private void SpawnBullet(AttackContext context)
    {
        Bullet bullet = BulletPool.Instance.Get(bulletData, context.Origin, context.Direction);
        bullet.Initialize(context, bulletData);
    }

    private static int ResolveSpreadCount(AttackContext context, MutationGrade grade)
    {
        if (MutationEffectResolver.TryGetGradeData(context, MutationType.Spread, SkillTag.None,
                MutationTargetScope.Common, out var gradeData) && gradeData.Count > 0)
            return gradeData.Count;

        return grade switch
        {
            MutationGrade.Safe => 3,
            MutationGrade.Caution => 5,
            MutationGrade.Danger => 5,
            MutationGrade.Quarantine => 5,
            _ => 1
        };
    }

    private static float ResolveSpreadDamageMultiplier(AttackContext context, MutationGrade grade)
    {
        if (MutationEffectResolver.TryGetGradeData(context, MutationType.Spread, SkillTag.None,
                MutationTargetScope.Common, out var gradeData) && gradeData.DamageMultiplier > 0f)
            return gradeData.DamageMultiplier;

        return grade switch
        {
            MutationGrade.Safe => 0.7f,
            MutationGrade.Caution => 0.7f,
            MutationGrade.Danger => 1f,
            MutationGrade.Quarantine => 1f,
            _ => 1f
        };
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos).normalized;
    }
}
