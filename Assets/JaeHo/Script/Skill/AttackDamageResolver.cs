using UnityEngine;

public static class AttackDamageResolver
{
    public static bool TryApplyDamage(
        AttackContext context,
        Collider2D target,
        Vector2 hitPoint,
        Vector2 hitNormal,
        out AttackHitResult result)
    {
        result = default;

        if (target == null) return false;
        if (!CombatComponentUtility.TryGet(target, out IDamageable damageable)) return false;

        CombatComponentUtility.TryGet(target, out IHitPointStatus hitPointStatus);
        bool wasAliveBeforeHit = hitPointStatus == null || !hitPointStatus.IsDead;
        float hpBefore = hitPointStatus != null ? hitPointStatus.CurrentHp : -1f;

        bool isCritical = RollCritical(context);
        float appliedDamage = CalculateDamage(context, isCritical);
        damageable.TakeDamage(appliedDamage);

        float hpAfter = hitPointStatus != null ? hitPointStatus.CurrentHp : -1f;
        bool killedByHit = hitPointStatus != null && wasAliveBeforeHit && hitPointStatus.IsDead;

        result = new AttackHitResult(
            context,
            target,
            damageable,
            hitPoint,
            hitNormal,
            context.BaseDamage,
            appliedDamage,
            isCritical,
            wasAliveBeforeHit,
            killedByHit,
            hpBefore,
            hpAfter);

        context.SourceSkill?.ReportHitResult(result);
        return true;
    }

    private static bool RollCritical(AttackContext context)
    {
        if (context.CriticalChance <= 0f) return false;
        if (context.CriticalChance >= 1f) return true;

        return Random.value < context.CriticalChance;
    }

    private static float CalculateDamage(AttackContext context, bool isCritical)
    {
        float damage = Mathf.Max(0f, context.BaseDamage);

        if (isCritical)
            damage *= context.CriticalMultiplier;

        return damage;
    }
}
