using System.Collections.Generic;
using UnityEngine;

/// 태그별 동작:
///   Bounce    → 벽(지형) 충돌 시 반사벡터로 방향 전환, 횟수 소진 시 반환
///   Piercing  → 트리거 모드, 피격 목록으로 중복 방지, 횟수 소진 시 반환
///   Explosive → 착탄 시 범위 데미지
///   Poison    → 피격 대상에 ApplyPoison() 호출
///   Slow      → 피격 대상에 ApplySlow() 호출
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("--- Explosive Settings ---")]
    [Tooltip("Explosive 태그 시 폭발 범위")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private LayerMask explosionLayer;

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

    // 런타임 상태
    private BulletData _data;
    private AttackContext _context;

    private Vector2    _direction;
    private float      _traveledDistance;
    private int        _bounceCount;
    private int        _pierceCount;
    private int        _bounceDamageSteps;
    private bool       _hasUnlimitedBounce;
    private bool       _hasUnlimitedPierce;

    // 관통 시 같은 타겟 중복 피격 방지
    private readonly HashSet<Collider2D> _pierced = new();

    private Rigidbody2D _rb;
    private Collider2D  _col;

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        _rb.gravityScale  = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    /// <summary>
    /// BulletPool.Get() 직후 반드시 호출.
    /// </summary>
    public void Initialize(AttackContext context, BulletData data)
    {
        _data = data;
        _context = context;
        _direction = context.Direction;

        _traveledDistance = 0f;
        _bounceDamageSteps = 0;
        ConfigureRicochet();
        ConfigurePierce();
        _pierced.Clear();

        // 관통이면 트리거(통과), 아니면 일반 충돌
        _col.isTrigger = HasPierce();

        _rb.linearVelocity = _direction * _data.speed;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void Initialize(Vector2 direction, float damage, SkillTag tags, BulletData data)
    {
        AttackContext context = new AttackContext(
            null,
            null,
            null,
            AttackRangeType.Ranged,
            AttackShapeType.Bullet,
            transform.position,
            direction,
            damage,
            tags);

        Initialize(context, data);
    }

    // 이동 & 거리 제한
    private void FixedUpdate()
    {
        _traveledDistance += _data.speed * Time.fixedDeltaTime;

        if (_traveledDistance >= _data.maxDistance)
            ReturnToPool();
    }

    // 일반 충돌 (Bounce용)
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.TryGetComponent<IDamageable>(out _))
        {
            ContactPoint2D contact = col.GetContact(0);
            ApplyHit(col.collider, contact.point, contact.normal);

            if (!HasRicochet())
            {
                ReturnToPool();
                return;
            }
        }

        if (HasRicochet())
        {
            if (!_hasUnlimitedBounce && _bounceCount <= 0)
            {
                ReturnToPool();
                return;
            }

            if (!_hasUnlimitedBounce)
                _bounceCount--;

            _bounceDamageSteps++;
            Vector2 normal = col.GetContact(0).normal;
            _direction = Vector2.Reflect(_direction, normal).normalized;
            _rb.linearVelocity = _direction * _data.speed;

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            ReturnToPool();
        }
    }

    // 트리거 충돌 (Piercing용)
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (_pierced.Contains(col)) return;
        if (!col.TryGetComponent<IDamageable>(out _)) return;

        _pierced.Add(col);
        ApplyHit(col, col.ClosestPoint(transform.position), -_direction);

        if (!_hasUnlimitedPierce)
            _pierceCount--;

        if (!_hasUnlimitedPierce && _pierceCount <= 0)
            ReturnToPool();
    }

    // 피격 공통 처리
    private void ApplyHit(Collider2D col, Vector2 hitPoint, Vector2 hitNormal)
    {
        AttackContext hitContext = _context.WithOriginAndDirection(transform.position, _direction);
        if (HasQuarantineRicochet())
            hitContext = hitContext.WithDamageMultiplier(1f + 0.1f * _bounceDamageSteps);

        if (!AttackDamageResolver.TryApplyDamage(hitContext, col, hitPoint, hitNormal, out var hitResult))
            return;

        TryApplyFollowUp(hitResult, col);
        TryExplode(hitResult);

        TryApplySlow(hitResult, col);
        TryApplyPoison(hitResult, col);
    }

    private void ConfigureRicochet()
    {
        MutationGrade grade = GetMutationGrade(
            MutationType.Ricochet,
            SkillTag.Bounce,
            MutationTargetScope.RangedOnly);

        _hasUnlimitedBounce = grade >= MutationGrade.Danger;

        if (TryGetMutationData(MutationType.Ricochet, SkillTag.Bounce,
                MutationTargetScope.RangedOnly, out var gradeData) && gradeData.Count > 0)
        {
            _bounceCount = gradeData.Count;
            _hasUnlimitedBounce = gradeData.Unlimited;
            return;
        }

        _bounceCount = grade switch
        {
            MutationGrade.Safe => 1,
            MutationGrade.Caution => 3,
            MutationGrade.Danger => int.MaxValue,
            MutationGrade.Quarantine => int.MaxValue,
            _ => 0
        };
    }

    private void ConfigurePierce()
    {
        MutationGrade grade = GetMutationGrade(
            MutationType.Pierce,
            SkillTag.Piercing,
            MutationTargetScope.RangedOnly);

        _hasUnlimitedPierce = grade >= MutationGrade.Danger;

        if (TryGetMutationData(MutationType.Pierce, SkillTag.Piercing,
                MutationTargetScope.RangedOnly, out var gradeData) && gradeData.Count > 0)
        {
            _pierceCount = gradeData.Count;
            _hasUnlimitedPierce = gradeData.Unlimited;
            return;
        }

        _pierceCount = grade switch
        {
            MutationGrade.Safe => 1,
            MutationGrade.Caution => 3,
            MutationGrade.Danger => int.MaxValue,
            MutationGrade.Quarantine => int.MaxValue,
            _ => 0
        };
    }

    private void TryExplode(AttackHitResult hitResult)
    {
        MutationGrade grade = GetMutationGrade(
            MutationType.Explosion,
            SkillTag.Explosive,
            MutationTargetScope.Common);

        if (grade == MutationGrade.None) return;

        bool shouldExplode = grade switch
        {
            MutationGrade.Safe => hitResult.IsCritical,
            MutationGrade.Caution => hitResult.IsCritical,
            MutationGrade.Danger => hitResult.IsCritical || hitResult.KilledByHit,
            MutationGrade.Quarantine => true,
            _ => false
        };

        if (TryGetMutationData(MutationType.Explosion, SkillTag.Explosive,
                MutationTargetScope.Common, out var gradeData))
            shouldExplode = MutationEffectResolver.ShouldTrigger(gradeData, hitResult);

        if (shouldExplode)
            Explode(ResolveExplosionDamage(grade));
    }

    private void TryApplySlow(AttackHitResult hitResult, Collider2D col)
    {
        MutationGrade grade = GetMutationGrade(MutationType.Slow, SkillTag.Slow, MutationTargetScope.Common);
        if (grade == MutationGrade.None) return;
        if (!col.TryGetComponent<ISlowable>(out var slowable)) return;

        float duration = ResolveSlowDuration(grade, slowDuration);
        if (TryGetMutationData(MutationType.Slow, SkillTag.Slow, MutationTargetScope.Common, out var gradeData))
        {
            if (!MutationEffectResolver.ShouldTrigger(gradeData, hitResult)) return;
            if (gradeData.Duration > 0f)
                duration = gradeData.Duration;
        }

        slowable.ApplySlow(slowMultiplier, duration);
    }

    private void TryApplyPoison(AttackHitResult hitResult, Collider2D col)
    {
        MutationGrade grade = GetMutationGrade(MutationType.Poison, SkillTag.Poison, MutationTargetScope.Common);
        if (grade == MutationGrade.None) return;
        if (!col.TryGetComponent<IPoisonable>(out var poisonable)) return;

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

        if (TryGetMutationData(MutationType.Poison, SkillTag.Poison,
                MutationTargetScope.Common, out var gradeData))
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

    private void TryApplyFollowUp(AttackHitResult hitResult, Collider2D col)
    {
        if (!MutationEffectResolver.TryGetFollowUpMultiplier(hitResult, out float multiplier)) return;
        if (!col.TryGetComponent<IDamageable>(out var damageable)) return;

        damageable.TakeDamage(hitResult.AppliedDamage * multiplier);
    }

    // 폭발
    private void Explode(float explosionDamage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, explosionRadius, explosionLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(explosionDamage);
        }
    }

    private void ReturnToPool()
    {
        _rb.linearVelocity = Vector2.zero;
        BulletPool.Instance.Return(this, _data);
    }

    private bool HasRicochet()
    {
        return GetMutationGrade(MutationType.Ricochet, SkillTag.Bounce,
            MutationTargetScope.RangedOnly) != MutationGrade.None;
    }

    private bool HasPierce()
    {
        return GetMutationGrade(MutationType.Pierce, SkillTag.Piercing,
            MutationTargetScope.RangedOnly) != MutationGrade.None;
    }

    private bool HasQuarantineRicochet()
    {
        return GetMutationGrade(MutationType.Ricochet, SkillTag.Bounce,
            MutationTargetScope.RangedOnly) == MutationGrade.Quarantine;
    }

    private MutationGrade GetMutationGrade(
        MutationType mutationType,
        SkillTag legacyTag,
        MutationTargetScope fallbackScope)
    {
        return MutationEffectResolver.GetGrade(_context, mutationType, legacyTag, fallbackScope);
    }

    private bool TryGetMutationData(
        MutationType mutationType,
        SkillTag legacyTag,
        MutationTargetScope fallbackScope,
        out MutationGradeData gradeData)
    {
        return MutationEffectResolver.TryGetGradeData(
            _context, mutationType, legacyTag, fallbackScope, out gradeData);
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_context.HasTag(SkillTag.Explosive)) return;
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
