using UnityEngine;

/// <summary>
/// 실제로 대상에게 공격이 적용된 결과.
/// 변이 시스템은 이 데이터를 보고 적중/처치/공격 타입 조건을 판단한다.
/// </summary>
public readonly struct AttackHitResult
{
    public readonly AttackContext Context;
    public readonly Collider2D TargetCollider;
    public readonly IDamageable Damageable;
    public readonly Vector2 HitPoint;
    public readonly Vector2 HitNormal;
    public readonly float BaseDamage;
    public readonly float AppliedDamage;
    public readonly bool IsCritical;
    public readonly bool WasAliveBeforeHit;
    public readonly bool KilledByHit;
    public readonly float TargetHpBefore;
    public readonly float TargetHpAfter;

    public AttackHitResult(
        AttackContext context,
        Collider2D targetCollider,
        IDamageable damageable,
        Vector2 hitPoint,
        Vector2 hitNormal,
        float baseDamage,
        float appliedDamage,
        bool isCritical,
        bool wasAliveBeforeHit,
        bool killedByHit,
        float targetHpBefore,
        float targetHpAfter)
    {
        Context = context;
        TargetCollider = targetCollider;
        Damageable = damageable;
        HitPoint = hitPoint;
        HitNormal = hitNormal;
        BaseDamage = baseDamage;
        AppliedDamage = appliedDamage;
        IsCritical = isCritical;
        WasAliveBeforeHit = wasAliveBeforeHit;
        KilledByHit = killedByHit;
        TargetHpBefore = targetHpBefore;
        TargetHpAfter = targetHpAfter;
    }
}
