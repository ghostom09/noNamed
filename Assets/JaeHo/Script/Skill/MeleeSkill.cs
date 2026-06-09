using UnityEngine;

/// <summary>
/// 근거리 범위 판정 스킬의 베이스 클래스.
/// SkillC(부채꼴), SkillD(전방 박스)가 상속.
/// Bullet 없이 Physics2D로 직접 범위 판정.
/// </summary>
public abstract class MeleeSkill : SkillBase
{
    [Header("--- Melee Settings ---")]
    [SerializeField] protected float attackRadius = 3f;
    [SerializeField] private int maxHitBufferSize = 32;

    [Header("--- Hit Validation ---")]
    [Tooltip("벽 너머의 대상을 맞히지 않게 할지 여부")]
    [SerializeField] private bool requireLineOfSight = false;
    [Tooltip("시야 판정에 사용할 장애물 레이어")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("--- Projectile Absorb Settings ---")]
    [SerializeField] private LayerMask projectileAbsorbLayer;
    [SerializeField, Min(1)] private int maxAbsorbBufferSize = 24;

    [Header("--- Slow Settings ---")]
    [Tooltip("Slow 태그 시 속도 배율 (0~1)")]
    [SerializeField] private float slowMultiplier = 0.5f;
    [Tooltip("Slow 지속 시간 (초)")]
    [SerializeField] private float slowDuration = 2f;

    [Header("--- Poison Settings ---")]
    [Tooltip("Poison 태그 시 틱당 데미지")]
    [SerializeField] private float poisonDamagePerTick = 5f;
    [Tooltip("Poison 지속 시간 (초)")]
    [SerializeField] private float poisonDuration = 3f;
    [Tooltip("Poison 틱 간격 (초)")]
    [SerializeField] private float poisonTickInterval = 1f;

    private Collider2D[] _colliderBuffer;
    private Collider2D[] _projectileBuffer;
    private ContactFilter2D _targetFilter;
    private ContactFilter2D _projectileFilter;

    protected override void Awake()
    {
        base.Awake();
        _colliderBuffer = new Collider2D[Mathf.Max(1, maxHitBufferSize)];
        _projectileBuffer = new Collider2D[Mathf.Max(1, maxAbsorbBufferSize)];
        _targetFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = targetLayer,
            useTriggers = true
        };
        _projectileFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = projectileAbsorbLayer,
            useTriggers = true
        };
    }

    /// <summary>
    /// 원형 범위 안의 대상을 NonAlloc 방식으로 수집한다.
    /// 반환값은 _colliderBuffer 안에 들어온 대상 수다.
    /// </summary>
    protected int CollectTargetsInRadius(Vector2 origin, float radius)
    {
        return Physics2D.OverlapCircle(origin, radius, _targetFilter, _colliderBuffer);
    }

    /// <summary>
    /// 방향에 맞춰 회전된 직사각형 범위 안의 대상을 NonAlloc 방식으로 수집한다.
    /// size.x는 전방 길이, size.y는 너비로 사용한다.
    /// </summary>
    protected int CollectTargetsInOrientedBox(Vector2 origin, Vector2 direction, Vector2 size)
    {
        Vector2 normalizedDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        Vector2 center = origin + normalizedDirection * (size.x * 0.5f);
        float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;

        return Physics2D.OverlapBox(center, size, angle, _targetFilter, _colliderBuffer);
    }

    protected Collider2D GetCollectedTarget(int index)
    {
        return _colliderBuffer[index];
    }

    protected void SortCollectedTargetsByDistance(Vector2 origin, int hitCount)
    {
        for (int i = 1; i < hitCount; i++)
        {
            Collider2D current = _colliderBuffer[i];
            float currentDistance = ((Vector2)current.transform.position - origin).sqrMagnitude;
            int j = i - 1;

            while (j >= 0)
            {
                float previousDistance = ((Vector2)_colliderBuffer[j].transform.position - origin).sqrMagnitude;
                if (previousDistance <= currentDistance) break;

                _colliderBuffer[j + 1] = _colliderBuffer[j];
                j--;
            }

            _colliderBuffer[j + 1] = current;
        }
    }

    /// <summary>
    /// 두 방향 사이의 각도를 계산해 부채꼴 범위 안에 있는지 판별.
    /// </summary>
    protected bool IsInFOV(Vector2 origin, Vector2 targetPos,
                            Vector2 forward, float halfAngle)
    {
        Vector2 toTarget = (targetPos - origin).normalized;
        float angle = Vector2.Angle(forward, toTarget);
        return angle <= halfAngle;
    }

    protected bool HasLineOfSight(Vector2 origin, Collider2D target)
    {
        if (!requireLineOfSight) return true;

        Vector2 targetPoint = target.bounds.center;
        Vector2 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f) return true;

        RaycastHit2D hit = Physics2D.Raycast(origin, toTarget / distance, distance, obstacleLayer);
        return hit.collider == null;
    }

    protected void AbsorbProjectilesInRadius(AttackContext context, Vector2 origin, float radius)
    {
        if (!MutationEffectResolver.TryGetProjectileAbsorbData(
                context, out bool reflect, out float reflectedDamageMultiplier))
            return;

        _projectileFilter.layerMask = projectileAbsorbLayer;
        int hitCount = Physics2D.OverlapCircle(origin, radius, _projectileFilter, _projectileBuffer);
        ApplyProjectileAbsorb(context, hitCount, reflect, reflectedDamageMultiplier);
    }

    protected void AbsorbProjectilesInOrientedBox(AttackContext context, Vector2 origin, Vector2 direction, Vector2 size)
    {
        if (!MutationEffectResolver.TryGetProjectileAbsorbData(
                context, out bool reflect, out float reflectedDamageMultiplier))
            return;

        Vector2 normalizedDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        Vector2 center = origin + normalizedDirection * (size.x * 0.5f);
        float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;

        _projectileFilter.layerMask = projectileAbsorbLayer;
        int hitCount = Physics2D.OverlapBox(center, size, angle, _projectileFilter, _projectileBuffer);
        ApplyProjectileAbsorb(context, hitCount, reflect, reflectedDamageMultiplier);
    }

    private void ApplyProjectileAbsorb(
        AttackContext context,
        int hitCount,
        bool reflect,
        float reflectedDamageMultiplier)
    {
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D projectile = _projectileBuffer[i];
            if (projectile == null) continue;
            if (!projectile.TryGetComponent<IAbsorbableProjectile>(out var absorbable)) continue;

            if (reflect)
                absorbable.Reflect(context.Attacker, context.Direction, reflectedDamageMultiplier);
            else
                absorbable.Absorb(context.Attacker);
        }
    }

    /// <summary>
    /// IDamageable에 데미지 적용. 태그 분기(Poison, Slow 등)도 여기서 처리.
    /// </summary>
    protected bool ApplyDamage(AttackContext context, Collider2D col, Vector2 hitPoint)
    {
        if (!AttackDamageResolver.TryApplyDamage(
                context,
                col,
                hitPoint,
                Vector2.zero,
                out var hitResult))
            return false;

        TryApplyFollowUp(hitResult, col);
        TryExplode(context, hitResult, col);
        TryApplyKnockback(hitResult, col);
        TryApplyStun(hitResult, col);
        TryApplyBind(hitResult, col);
        TryApplySlow(context, hitResult, col);
        TryApplyPoison(context, hitResult, col);

        return true;
    }

    private void TryApplyFollowUp(AttackHitResult hitResult, Collider2D col)
    {
        if (!MutationEffectResolver.TryGetFollowUpMultiplier(hitResult, out float multiplier)) return;
        if (!CombatComponentUtility.TryGet(col, out IDamageable damageable)) return;

        damageable.TakeDamage(hitResult.AppliedDamage * multiplier);
    }

    private void TryApplyKnockback(AttackHitResult hitResult, Collider2D col)
    {
        if (!MutationEffectResolver.TryGetKnockbackData(hitResult, out float impulse,
                out float collisionDamage, out float extraTargetDamage, out bool stunOnCollision))
            return;

        if (!CombatComponentUtility.TryGet(col, out IKnockbackable knockbackable)) return;

        Vector2 direction = ((Vector2)col.bounds.center - hitResult.Context.Origin).normalized;
        if (direction.sqrMagnitude <= 0f)
            direction = hitResult.Context.Direction;

        knockbackable.ApplyKnockback(direction, impulse, collisionDamage, extraTargetDamage, stunOnCollision);
    }

    private void TryApplyStun(AttackHitResult hitResult, Collider2D col)
    {
        if (!MutationEffectResolver.TryGetStunDuration(hitResult, out float duration)) return;
        if (CombatComponentUtility.TryGet(col, out IStunnable stunnable))
            stunnable.ApplyStun(duration);
    }

    private void TryApplyBind(AttackHitResult hitResult, Collider2D col)
    {
        if (!MutationEffectResolver.TryGetBindData(hitResult, out float duration,
                out float damagePerTick, out float tickInterval)) return;

        if (CombatComponentUtility.TryGet(col, out IBindable bindable))
            bindable.ApplyBind(duration, damagePerTick, tickInterval);
    }

    private void TryExplode(AttackContext context, AttackHitResult hitResult, Collider2D originalTarget)
    {
        MutationGrade grade = GetMutationGrade(context, MutationType.Explosion, SkillTag.Explosive);
        if (grade == MutationGrade.None) return;

        bool shouldExplode = grade switch
        {
            MutationGrade.Safe => hitResult.IsCritical,
            MutationGrade.Caution => hitResult.IsCritical,
            MutationGrade.Danger => hitResult.IsCritical || hitResult.KilledByHit,
            MutationGrade.Quarantine => true,
            _ => false
        };

        float damage = ResolveExplosionDamage(grade);
        if (TryGetMutationData(context, MutationType.Explosion, SkillTag.Explosive, out var gradeData))
        {
            shouldExplode = MutationEffectResolver.ShouldTrigger(gradeData, hitResult);
            if (gradeData.BonusDamage > 0f)
                damage = gradeData.BonusDamage;
        }

        if (!shouldExplode) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitResult.HitPoint, attackRadius, targetLayer);
        foreach (var hit in hits)
        {
            if (hit == null || hit == originalTarget) continue;
            if (CombatComponentUtility.TryGet(hit, out IDamageable damageable))
                damageable.TakeDamage(damage);
        }
    }

    private void TryApplySlow(AttackContext context, AttackHitResult hitResult, Collider2D col)
    {
        MutationGrade grade = GetMutationGrade(context, MutationType.Slow, SkillTag.Slow);
        if (grade == MutationGrade.None) return;
        if (!CombatComponentUtility.TryGet(col, out ISlowable slowable)) return;

        float duration = ResolveSlowDuration(grade, slowDuration);
        if (TryGetMutationData(context, MutationType.Slow, SkillTag.Slow, out var gradeData))
        {
            if (!MutationEffectResolver.ShouldTrigger(gradeData, hitResult)) return;
            if (gradeData.Duration > 0f)
                duration = gradeData.Duration;
        }

        slowable.ApplySlow(slowMultiplier, duration);
    }

    private void TryApplyPoison(AttackContext context, AttackHitResult hitResult, Collider2D col)
    {
        MutationGrade grade = GetMutationGrade(context, MutationType.Poison, SkillTag.Poison);
        if (grade == MutationGrade.None) return;
        if (!CombatComponentUtility.TryGet(col, out IPoisonable poisonable)) return;

        bool shouldPoison = grade switch
        {
            MutationGrade.Safe => hitResult.IsCritical,
            MutationGrade.Caution => hitResult.IsCritical,
            MutationGrade.Danger => hitResult.IsCritical || hitResult.KilledByHit,
            MutationGrade.Quarantine => hitResult.IsCritical || hitResult.KilledByHit,
            _ => false
        };

        float damagePerTick = ResolvePoisonDamagePerTick(grade, poisonDamagePerTick);
        float duration = ResolvePoisonDuration(grade, poisonDuration);
        float tickInterval = poisonTickInterval;

        if (TryGetMutationData(context, MutationType.Poison, SkillTag.Poison, out var gradeData))
        {
            shouldPoison = MutationEffectResolver.ShouldTrigger(gradeData, hitResult);
            if (gradeData.BonusDamage > 0f)
                damagePerTick = gradeData.BonusDamage;
            if (gradeData.Duration > 0f)
                duration = gradeData.Duration;
            if (gradeData.TickInterval > 0f)
                tickInterval = gradeData.TickInterval;
        }

        if (shouldPoison)
            poisonable.ApplyPoison(damagePerTick, duration, tickInterval);
    }

    private static MutationGrade GetMutationGrade(
        AttackContext context,
        MutationType mutationType,
        SkillTag legacyTag)
    {
        return MutationEffectResolver.GetGrade(
            context, mutationType, legacyTag, MutationTargetScope.Common);
    }

    private static bool TryGetMutationData(
        AttackContext context,
        MutationType mutationType,
        SkillTag legacyTag,
        out MutationGradeData gradeData)
    {
        return MutationEffectResolver.TryGetGradeData(
            context, mutationType, legacyTag, MutationTargetScope.Common, out gradeData);
    }

    private static float ResolveExplosionDamage(MutationGrade grade)
    {
        return grade switch
        {
            MutationGrade.Safe => 6f,
            MutationGrade.Caution => 8f,
            MutationGrade.Danger => 8f,
            MutationGrade.Quarantine => 8f,
            _ => 0f
        };
    }

    private static float ResolveSlowDuration(MutationGrade grade, float fallbackDuration)
    {
        return grade switch
        {
            MutationGrade.Safe => 1.5f,
            MutationGrade.Caution => 2f,
            MutationGrade.Danger => 3f,
            MutationGrade.Quarantine => 3f,
            _ => fallbackDuration
        };
    }

    private static float ResolvePoisonDuration(MutationGrade grade, float fallbackDuration)
    {
        return grade switch
        {
            MutationGrade.Safe => 3f,
            MutationGrade.Caution => 5f,
            MutationGrade.Danger => 5f,
            MutationGrade.Quarantine => 5f,
            _ => fallbackDuration
        };
    }

    private static float ResolvePoisonDamagePerTick(MutationGrade grade, float fallbackDamage)
    {
        return grade == MutationGrade.None ? fallbackDamage : 2f;
    }
}
