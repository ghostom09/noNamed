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

    [Header("--- Laser Settings ---")]
    [SerializeField, Min(1)] private int maxLaserHits = 32;
    [SerializeField] private LayerMask laserObstacleLayer;

    private RaycastHit2D[] _laserHitBuffer;
    private ContactFilter2D _laserFilter;

    protected override void Awake()
    {
        base.Awake();

        if (bulletData == null)
            Debug.LogError($"[{name}] BulletData가 할당되지 않음");

        _laserHitBuffer = new RaycastHit2D[Mathf.Max(1, maxLaserHits)];
        _laserFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = targetLayer.value | laserObstacleLayer.value,
            useTriggers = true
        };
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

        if (MutationEffectResolver.IsLaserPierce(context))
        {
            FireLaser(context.WithShape(AttackShapeType.Laser));
            return;
        }

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

    private void FireLaser(AttackContext context)
    {
        _laserFilter.layerMask = targetLayer.value | laserObstacleLayer.value;

        int hitCount = Physics2D.Raycast(
            context.Origin, context.Direction, _laserFilter, _laserHitBuffer, attackDistance);

        SortLaserHits(hitCount);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _laserHitBuffer[i];
            if (hit.collider == null) continue;

            if ((laserObstacleLayer.value & (1 << hit.collider.gameObject.layer)) != 0)
                break;

            if (!AttackDamageResolver.TryApplyDamage(
                    context.WithOriginAndDirection(hit.point, context.Direction),
                    hit.collider,
                    hit.point,
                    hit.normal,
                    out var hitResult))
                continue;

            ApplyLaserFollowUp(hitResult, hit.collider);
            ApplyLaserStun(hitResult, hit.collider);
            ApplyLaserBind(hitResult, hit.collider);
        }
    }

    private void SortLaserHits(int hitCount)
    {
        for (int i = 1; i < hitCount; i++)
        {
            RaycastHit2D current = _laserHitBuffer[i];
            int j = i - 1;

            while (j >= 0 && _laserHitBuffer[j].distance > current.distance)
            {
                _laserHitBuffer[j + 1] = _laserHitBuffer[j];
                j--;
            }

            _laserHitBuffer[j + 1] = current;
        }
    }

    private static void ApplyLaserFollowUp(AttackHitResult hitResult, Collider2D collider)
    {
        if (!MutationEffectResolver.TryGetFollowUpMultiplier(hitResult, out float multiplier)) return;
        if (collider.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(hitResult.AppliedDamage * multiplier);
    }

    private static void ApplyLaserStun(AttackHitResult hitResult, Collider2D collider)
    {
        if (!MutationEffectResolver.TryGetStunDuration(hitResult, out float duration)) return;
        if (collider.TryGetComponent<IStunnable>(out var stunnable))
            stunnable.ApplyStun(duration);
    }

    private static void ApplyLaserBind(AttackHitResult hitResult, Collider2D collider)
    {
        if (!MutationEffectResolver.TryGetBindData(hitResult, out float duration,
                out float damagePerTick, out float tickInterval)) return;

        if (collider.TryGetComponent<IBindable>(out var bindable))
            bindable.ApplyBind(duration, damagePerTick, tickInterval);
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
