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
    [SerializeField] private int maxExplosionTargets = 10;

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

    [Header("--- Homing Settings ---")]
    [SerializeField] private float homingSearchRadius = 12f;
    [SerializeField] private float homingTurnSpeed = 720f;
    [SerializeField] private int maxHomingTargets = 16;

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
    private float      _homingEndsAt;
    private bool       _homingUntilHit;

    // 관통 시 같은 타겟 중복 피격 방지
    private readonly HashSet<Collider2D> _pierced = new();

    private Collider2D[] _explosionOverlapBuffer;
    private ContactFilter2D _explosionFilter;
    private Collider2D[] _homingOverlapBuffer;
    private ContactFilter2D _homingFilter;

    private Rigidbody2D _rb;
    private Collider2D  _col;

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        _rb.gravityScale  = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _explosionOverlapBuffer = new Collider2D[maxExplosionTargets];
        _homingOverlapBuffer = new Collider2D[maxHomingTargets];
        _explosionFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = explosionLayer,
            useTriggers = true
        };
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
        ConfigureHoming();
        _pierced.Clear();

        // 관통이면 트리거(통과), 아니면 일반 충돌
        _col.isTrigger = true;

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
        UpdateHoming();
        _traveledDistance += _data.speed * Time.fixedDeltaTime;

        if (_traveledDistance >= _data.maxDistance)
            ReturnToPool();
    }

    private void UpdateHoming()
    {
        if (!_homingUntilHit && Time.time > _homingEndsAt) return;
        if (_context.TargetLayer.value == 0) return;

        _homingFilter.useLayerMask = true;
        _homingFilter.layerMask = _context.TargetLayer;
        _homingFilter.useTriggers = true;

        int hitCount = Physics2D.OverlapCircle(
            transform.position, homingSearchRadius, _homingFilter, _homingOverlapBuffer);

        Collider2D target = FindClosestHomingTarget(hitCount);
        if (target == null) return;

        Vector2 toTarget = ((Vector2)target.bounds.center - (Vector2)transform.position).normalized;
        float maxRadiansDelta = homingTurnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
        _direction = Vector3.RotateTowards(_direction, toTarget, maxRadiansDelta, 0f).normalized;
        _rb.linearVelocity = _direction * _data.speed;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Collider2D FindClosestHomingTarget(int hitCount)
    {
        Collider2D closest = null;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D candidate = _homingOverlapBuffer[i];
            if (candidate == null || _pierced.Contains(candidate)) continue;

            float distance = ((Vector2)candidate.bounds.center - (Vector2)transform.position).sqrMagnitude;
            if (distance >= closestDistance) continue;

            closest = candidate;
            closestDistance = distance;
        }

        return closest;
    }

    // 일반 충돌 (Bounce용)
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (ShouldIgnoreCollider(col.collider))
            return;

        if (CombatComponentUtility.TryGet(col.collider, out IDamageable _))
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
        if (ShouldIgnoreCollider(col)) return;

        if (!CombatComponentUtility.TryGet(col, out IDamageable _))
        {
            HandleWorldTrigger(col);
            return;
        }

        bool hasPierce = HasPierce();
        if (hasPierce)
            _pierced.Add(col);

        ApplyHit(col, col.ClosestPoint(transform.position), -_direction);

        if (!hasPierce)
        {
            if (HasRicochet())
                RicochetFrom(col);
            else
                ReturnToPool();

            return;
        }

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

        TryApplyStun(hitResult, col);
        TryApplyBind(hitResult, col);
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

        int dataPierceCount = _data != null ? Mathf.Max(1, _data.maxPierceCount) : 1;
        _pierceCount = grade switch
        {
            MutationGrade.Safe => dataPierceCount,
            MutationGrade.Caution => Mathf.Max(dataPierceCount, 3),
            MutationGrade.Danger => int.MaxValue,
            MutationGrade.Quarantine => int.MaxValue,
            _ => 0
        };
    }

    private void ConfigureHoming()
    {
        _homingEndsAt = 0f;
        _homingUntilHit = false;

        if (!MutationEffectResolver.TryGetHomingData(_context, out float duration))
            return;

        _homingUntilHit = float.IsPositiveInfinity(duration);
        _homingEndsAt = _homingUntilHit ? float.PositiveInfinity : Time.time + duration;
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

        float damage = ResolveExplosionDamage(grade);
        if (TryGetMutationData(MutationType.Explosion, SkillTag.Explosive,
                MutationTargetScope.Common, out var gradeData))
        {
            shouldExplode = MutationEffectResolver.ShouldTrigger(gradeData, hitResult);
            if (gradeData.BonusDamage > 0f)
                damage = gradeData.BonusDamage;
        }

        if (shouldExplode)
            Explode(damage, hitResult);
    }

    private void TryApplySlow(AttackHitResult hitResult, Collider2D col)
    {
        MutationGrade grade = GetMutationGrade(MutationType.Slow, SkillTag.Slow, MutationTargetScope.Common);
        if (grade == MutationGrade.None) return;
        if (!CombatComponentUtility.TryGet(col, out ISlowable slowable)) return;

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

    private void TryApplyFollowUp(AttackHitResult hitResult, Collider2D col)
    {
        if (!MutationEffectResolver.TryGetFollowUpMultiplier(hitResult, out float multiplier)) return;
        IDamageable damageable = hitResult.Damageable;
        if (damageable == null) return;

        float followUpDamage = hitResult.AppliedDamage * multiplier;
        Debug.Log($"[FollowUp Hit] {hitResult.Context.SourceSkill?.name ?? "UnknownSkill"} -> {col.name} damage:{followUpDamage:0.##} ({multiplier:0.##}x)", this);
        damageable.TakeDamage(followUpDamage);
    }

    // 폭발
    private void Explode(float explosionDamage, AttackHitResult sourceHit)
    {
        int hitsCount = Physics2D.OverlapCircle(
            sourceHit.HitPoint, explosionRadius, _explosionFilter, _explosionOverlapBuffer);

        for (int i = 0; i < hitsCount; i++)
        {
            Collider2D target = _explosionOverlapBuffer[i];
            if (target == null || target == sourceHit.TargetCollider || ShouldIgnoreCollider(target))
                continue;

            Vector2 hitPoint = target.ClosestPoint(sourceHit.HitPoint);
            Vector2 hitNormal = ((Vector2)target.bounds.center - sourceHit.HitPoint).normalized;
            if (hitNormal.sqrMagnitude <= 0.0001f)
                hitNormal = sourceHit.Context.Direction;

            AttackContext explosionContext = sourceHit.Context
                .WithShape(AttackShapeType.Bullet)
                .WithOriginAndDirection(sourceHit.HitPoint, hitNormal)
                .WithBaseDamage(explosionDamage);

            AttackDamageResolver.TryApplyDamage(explosionContext, target, hitPoint, hitNormal, out _);
        }
    }

    private bool ShouldIgnoreCollider(Collider2D col)
    {
        if (col == null)
            return true;

        if (_context.Attacker != null &&
            (col.gameObject == _context.Attacker || col.transform.IsChildOf(_context.Attacker.transform)))
        {
            return true;
        }

        if (IsWorldHitTarget(col))
            return false;

        if (!CombatComponentUtility.TryGet(col, out IDamageable damageable))
            return false;

        return !IsInTargetLayer(col.gameObject) && !IsDamageableInTargetLayer(damageable);
    }

    private bool IsInTargetLayer(GameObject target)
    {
        if (target == null)
            return false;

        if (_context.TargetLayer.value == 0)
            return true;

        return (_context.TargetLayer.value & (1 << target.layer)) != 0;
    }

    private bool IsDamageableInTargetLayer(IDamageable damageable)
    {
        if (_context.TargetLayer.value == 0)
            return true;

        if (damageable is Component component)
            return IsInTargetLayer(component.gameObject);

        return false;
    }

    private void ReturnToPool()
    {
        _rb.linearVelocity = Vector2.zero;
        BulletPool.Instance.Return(this, _data);
    }

    private void HandleWorldTrigger(Collider2D col)
    {
        if (!IsWorldHitTarget(col)) return;

        if (HasRicochet())
            RicochetFrom(col);
        else
            ReturnToPool();
    }

    private static bool IsWorldHitTarget(Collider2D col)
    {
        if (col == null) return false;
        if (col.CompareTag("Wall")) return true;

        int wallLayer = LayerMask.NameToLayer("Wall");
        return wallLayer >= 0 && col.gameObject.layer == wallLayer;
    }

    private void RicochetFrom(Collider2D col)
    {
        if (!_hasUnlimitedBounce && _bounceCount <= 0)
        {
            ReturnToPool();
            return;
        }

        if (!_hasUnlimitedBounce)
            _bounceCount--;

        _bounceDamageSteps++;

        Vector2 closest = col.ClosestPoint(transform.position);
        Vector2 normal = ((Vector2)transform.position - closest).normalized;
        if (normal.sqrMagnitude <= 0.0001f)
            normal = -_direction;

        _direction = Vector2.Reflect(_direction, normal).normalized;
        _rb.linearVelocity = _direction * _data.speed;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
