using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실제로 날아다니는 총알 오브젝트.
/// BulletPool.Get() 호출 후 Initialize()로 초기화해서 사용.
///
/// 태그별 동작:
///   Bounce    → 벽(지형) 충돌 시 반사벡터로 방향 전환, 횟수 소진 시 반환
///   Piercing  → 트리거 모드, 피격 목록으로 중복 방지, 횟수 소진 시 반환
///   Explosive → 착탄 시 범위 데미지 (Bounce/Piercing과 중첩 가능)
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

    // 런타임 상태
    private BulletData    _data;
    private SkillTag      _tags;
    private float         _damage;

    private Vector2       _direction;
    private float         _traveledDistance;
    private int           _bounceCount;
    private int           _pierceCount;

    // 관통 시 같은 타겟 중복 피격 방지
    private readonly HashSet<Collider2D> _pierced = new();

    private Rigidbody2D   _rb;
    private Collider2D    _col;

    // 초기화
    private void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        // 중력 없는 2D 발사체
        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    /// <summary>
    /// BulletPool.Get() 직후 반드시 호출.
    /// </summary>
    public void Initialize(Vector2 direction, float damage, SkillTag tags, BulletData data)
    {
        _data      = data;
        _tags      = tags;
        _damage    = damage;
        _direction = direction.normalized;

        _traveledDistance = 0f;
        _bounceCount      = data.maxBounceCount;
        _pierceCount      = data.maxPierceCount;
        _pierced.Clear();

        // 관통이면 트리거(통과), 아니면 일반 충돌
        _col.isTrigger = HasTag(SkillTag.Piercing);

        // Rigidbody로 이동
        _rb.linearVelocity = _direction * _data.speed;

        // 진행 방향으로 스프라이트 회전
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // 이동 & 거리 제한
    private void FixedUpdate()
    {
        float step = _data.speed * Time.fixedDeltaTime;
        _traveledDistance += step;

        if (_traveledDistance >= _data.maxDistance)
            ReturnToPool();
    }

    // 충돌 처리 (일반 충돌 - Bounce용)
    private void OnCollisionEnter2D(Collision2D col)
    {
        // 데미지 대상이면 피격 처리
        if (col.collider.TryGetComponent<IDamageable>(out var damageable))
        {
            ApplyHit(col.collider, damageable);

            if (!HasTag(SkillTag.Bounce))
            {
                ReturnToPool();
                return;
            }
        }

        // 벽(지형) 충돌 → 바운스
        if (HasTag(SkillTag.Bounce))
        {
            if (_bounceCount <= 0)
            {
                ReturnToPool();
                return;
            }

            _bounceCount--;
            Vector2 normal = col.contacts[0].normal;
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

    // 트리거 처리 (관통용)
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (_pierced.Contains(col)) return;

        if (!col.TryGetComponent<IDamageable>(out var damageable)) return;

        _pierced.Add(col);
        ApplyHit(col, damageable);

        _pierceCount--;
        if (_pierceCount <= 0)
            ReturnToPool();
    }

    // 피격 공통 처리
    private void ApplyHit(Collider2D col, IDamageable damageable)
    {
        damageable.TakeDamage(_damage);

        if (HasTag(SkillTag.Explosive))
            Explode();

        if (HasTag(SkillTag.Poison) && col.TryGetComponent<IPoisonable>(out var p))
            p.ApplyPoison();

        if (HasTag(SkillTag.Slow) && col.TryGetComponent<ISlowable>(out var s))
            s.ApplySlow();
    }

    // 폭발
    private void Explode()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position,
                                                        explosionRadius, explosionLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(_damage * 0.5f); // 폭발 데미지는 직격의 50%
        }
    }

    // 풀 반환
    private void ReturnToPool()
    {
        _rb.linearVelocity = Vector2.zero;
        BulletPool.Instance.Return(this, _data);
    }

    private bool HasTag(SkillTag tag) => (_tags & tag) != 0;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!HasTag(SkillTag.Explosive)) return;
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}