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

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 원형 범위 안의 모든 콜라이더를 가져온 뒤
    /// 각도 필터, 태그 분기 등을 서브클래스에서 처리.
    /// </summary>
    protected Collider2D[] GetTargetsInRadius(Vector2 origin, float radius)
    {
        return Physics2D.OverlapCircleAll(origin, radius, targetLayer);
    }

    /// <summary>
    /// 박스 범위 안의 모든 콜라이더 반환.
    /// SkillD(전방 직선) 등에서 사용.
    /// </summary>
    protected RaycastHit2D[] GetTargetsInBox(Vector2 origin, Vector2 direction,
        Vector2 size, float distance)
    {
        return Physics2D.BoxCastAll(origin, size, 0f, direction, distance, targetLayer);
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

    /// <summary>
    /// IDamageable에 데미지 적용. 태그 분기(Poison, Slow 등)도 여기서 처리.
    /// </summary>
    protected void ApplyDamage(Collider2D col)
    {
        if (!col.TryGetComponent<IDamageable>(out var damageable)) return;

        damageable.TakeDamage(attackDamage);

        // 태그별 추가 효과 (ISlowable, IPoisonable 등 인터페이스 추가 시 확장)
        if (HasTag(SkillTag.Slow) && col.TryGetComponent<ISlowable>(out var slowable))
            slowable.ApplySlow();

        if (HasTag(SkillTag.Poison) && col.TryGetComponent<IPoisonable>(out var poisonable))
            poisonable.ApplyPoison();
    }
}