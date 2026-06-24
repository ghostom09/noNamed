using UnityEngine;

public static class MutationEffectResolver
{
    public static MutationGrade GetGrade(
        AttackContext context,
        MutationType mutationType,
        SkillTag legacyTag,
        MutationTargetScope fallbackScope)
    {
        if (!CanApplyTo(context, mutationType, fallbackScope))
            return MutationGrade.None;

        if (context.Mutations != null && context.Mutations.HasMutation(mutationType))
            return context.Mutations.GetGrade(mutationType);

        return context.HasTag(legacyTag) ? MutationGrade.Safe : MutationGrade.None;
    }

    public static bool IsActive(
        AttackContext context,
        MutationType mutationType,
        SkillTag legacyTag,
        MutationTargetScope fallbackScope)
    {
        return GetGrade(context, mutationType, legacyTag, fallbackScope) != MutationGrade.None;
    }

    public static bool TryGetGradeData(
        AttackContext context,
        MutationType mutationType,
        SkillTag legacyTag,
        MutationTargetScope fallbackScope,
        out MutationGradeData gradeData)
    {
        gradeData = null;

        if (GetGrade(context, mutationType, legacyTag, fallbackScope) == MutationGrade.None)
            return false;

        return context.Mutations != null &&
               context.Mutations.TryGetGradeData(mutationType, out gradeData);
    }

    public static bool ShouldTrigger(MutationGradeData gradeData, AttackHitResult hitResult)
    {
        if (gradeData == null) return true;

        return gradeData.TriggerType switch
        {
            MutationTriggerType.Passive => true,
            MutationTriggerType.OnAttack => true,
            MutationTriggerType.OnHit => true,
            MutationTriggerType.OnCriticalHit => hitResult.IsCritical,
            MutationTriggerType.OnKill => hitResult.KilledByHit,
            MutationTriggerType.OnProjectileExpired => false,
            MutationTriggerType.OnCriticalHitOrKill => hitResult.IsCritical || hitResult.KilledByHit,
            _ => false
        };
    }

    public static bool TryGetFollowUpMultiplier(AttackHitResult hitResult, out float multiplier)
    {
        AttackContext context = hitResult.Context;
        multiplier = 0f;

        MutationGrade grade = GetGrade(
            context,
            MutationType.FollowUp,
            SkillTag.FollowUp,
            MutationTargetScope.Common);

        if (grade == MutationGrade.None) return false;

        if (TryGetGradeData(context, MutationType.FollowUp, SkillTag.FollowUp,
                MutationTargetScope.Common, out var gradeData))
        {
            if (!ShouldTrigger(gradeData, hitResult)) return false;

            multiplier = gradeData.DamageMultiplier > 0f
                ? gradeData.DamageMultiplier
                : ResolveFollowUpMultiplier(grade);
            return multiplier > 0f;
        }

        multiplier = ResolveFollowUpMultiplier(grade);
        return multiplier > 0f;
    }

    public static bool TryGetStunDuration(AttackHitResult hitResult, out float duration)
    {
        AttackContext context = hitResult.Context;
        duration = 0f;

        MutationGrade grade = GetGrade(context, MutationType.Stun, SkillTag.Stun, MutationTargetScope.Common);
        if (grade == MutationGrade.None) return false;

        if (TryGetGradeData(context, MutationType.Stun, SkillTag.Stun,
                MutationTargetScope.Common, out var gradeData))
        {
            if (!ShouldTrigger(gradeData, hitResult)) return false;

            duration = gradeData.Duration > 0f
                ? gradeData.Duration
                : ResolveStunDuration(grade);
            return duration > 0f;
        }

        if (!hitResult.IsCritical) return false;

        duration = ResolveStunDuration(grade);
        return duration > 0f;
    }

    public static bool TryGetBindData(AttackHitResult hitResult, out float duration, out float damagePerTick, out float tickInterval)
    {
        AttackContext context = hitResult.Context;
        duration = 0f;
        damagePerTick = 0f;
        tickInterval = 0f;

        MutationGrade grade = GetGrade(context, MutationType.Bind, SkillTag.Bind, MutationTargetScope.Common);
        if (grade == MutationGrade.None) return false;

        if (TryGetGradeData(context, MutationType.Bind, SkillTag.Bind,
                MutationTargetScope.Common, out var gradeData))
        {
            if (!ShouldTrigger(gradeData, hitResult)) return false;

            duration = gradeData.Duration > 0f ? gradeData.Duration : ResolveBindDuration(grade);
            damagePerTick = gradeData.BonusDamage > 0f ? gradeData.BonusDamage : ResolveBindDamagePerTick(grade);
            tickInterval = gradeData.TickInterval > 0f ? gradeData.TickInterval : ResolveBindTickInterval(grade);
            return duration > 0f;
        }

        if (!hitResult.IsCritical) return false;

        duration = ResolveBindDuration(grade);
        damagePerTick = ResolveBindDamagePerTick(grade);
        tickInterval = ResolveBindTickInterval(grade);
        return duration > 0f;
    }

    public static bool TryGetKnockbackData(
        AttackHitResult hitResult,
        out float impulse,
        out float collisionDamage,
        out float extraTargetDamage,
        out bool stunOnCollision)
    {
        AttackContext context = hitResult.Context;
        impulse = 0f;
        collisionDamage = 0f;
        extraTargetDamage = 0f;
        stunOnCollision = false;

        MutationGrade grade = GetGrade(context, MutationType.Knockback, SkillTag.Knockback, MutationTargetScope.MeleeOnly);
        if (grade == MutationGrade.None) return false;

        if (TryGetGradeData(context, MutationType.Knockback, SkillTag.Knockback,
                MutationTargetScope.MeleeOnly, out var gradeData))
        {
            if (!ShouldTrigger(gradeData, hitResult)) return false;

            impulse = gradeData.Count > 0 ? gradeData.Count : ResolveKnockbackImpulse(grade);
            collisionDamage = gradeData.BonusDamage > 0f ? gradeData.BonusDamage : ResolveKnockbackCollisionDamage(grade);
            extraTargetDamage = ResolveKnockbackExtraTargetDamage(grade);
            stunOnCollision = grade >= MutationGrade.Quarantine;
            return impulse > 0f;
        }

        impulse = ResolveKnockbackImpulse(grade);
        collisionDamage = ResolveKnockbackCollisionDamage(grade);
        extraTargetDamage = ResolveKnockbackExtraTargetDamage(grade);
        stunOnCollision = grade >= MutationGrade.Quarantine;
        return impulse > 0f;
    }

    public static bool TryGetHomingData(AttackContext context, out float duration)
    {
        duration = 0f;

        MutationGrade grade = GetGrade(context, MutationType.Homing, SkillTag.Homing, MutationTargetScope.RangedOnly);
        if (grade == MutationGrade.None) return false;

        if (TryGetGradeData(context, MutationType.Homing, SkillTag.Homing,
                MutationTargetScope.RangedOnly, out var gradeData) && gradeData.Duration > 0f)
        {
            duration = gradeData.Duration;
            return true;
        }

        duration = grade switch
        {
            MutationGrade.Safe => 2f,
            MutationGrade.Caution => 3f,
            MutationGrade.Danger => 5f,
            MutationGrade.Quarantine => float.PositiveInfinity,
            _ => 0f
        };

        return duration > 0f;
    }

    public static bool IsLaserPierce(AttackContext context)
    {
        return GetGrade(context, MutationType.Pierce, SkillTag.Piercing,
            MutationTargetScope.RangedOnly) >= MutationGrade.Quarantine;
    }

    public static bool TryGetProjectileAbsorbData(
        AttackContext context,
        out bool reflect,
        out float reflectedDamageMultiplier)
    {
        reflect = false;
        reflectedDamageMultiplier = 0f;

        MutationGrade grade = GetGrade(context, MutationType.ProjectileAbsorb, SkillTag.ProjectileAbsorb,
            MutationTargetScope.MeleeOnly);
        if (grade == MutationGrade.None) return false;

        reflect = grade >= MutationGrade.Danger;
        reflectedDamageMultiplier = grade >= MutationGrade.Quarantine ? 1.3f : 1f;

        if (TryGetGradeData(context, MutationType.ProjectileAbsorb, SkillTag.ProjectileAbsorb,
                MutationTargetScope.MeleeOnly, out var gradeData))
        {
            reflect = gradeData.Grade >= MutationGrade.Danger;
            if (gradeData.DamageMultiplier > 0f)
                reflectedDamageMultiplier = gradeData.DamageMultiplier;
        }

        return true;
    }

    private static float ResolveFollowUpMultiplier(MutationGrade grade)
    {
        return grade switch
        {
            MutationGrade.Safe => 0.5f,
            MutationGrade.Caution => 1f,
            MutationGrade.Danger => 1.3f,
            MutationGrade.Quarantine => 1.3f,
            _ => 0f
        };
    }

    private static float ResolveStunDuration(MutationGrade grade)
    {
        return grade switch
        {
            MutationGrade.Safe => 0.5f,
            MutationGrade.Caution => 0.7f,
            MutationGrade.Danger => 1f,
            MutationGrade.Quarantine => 1f,
            _ => 0f
        };
    }

    private static float ResolveBindDuration(MutationGrade grade)
    {
        return grade switch
        {
            MutationGrade.Safe => 1f,
            MutationGrade.Caution => 2f,
            MutationGrade.Danger => 3f,
            MutationGrade.Quarantine => 3f,
            _ => 0f
        };
    }

    private static float ResolveBindDamagePerTick(MutationGrade grade)
    {
        return grade >= MutationGrade.Danger ? 2f : 0f;
    }

    private static float ResolveBindTickInterval(MutationGrade grade)
    {
        return grade >= MutationGrade.Danger ? 1f : 0f;
    }

    private static float ResolveKnockbackImpulse(MutationGrade grade)
    {
        return grade switch
        {
            MutationGrade.Safe => 6f,
            MutationGrade.Caution => 10f,
            MutationGrade.Danger => 10f,
            MutationGrade.Quarantine => 10f,
            _ => 0f
        };
    }

    private static float ResolveKnockbackCollisionDamage(MutationGrade grade)
    {
        return grade >= MutationGrade.Danger ? 8f : 0f;
    }

    private static float ResolveKnockbackExtraTargetDamage(MutationGrade grade)
    {
        return grade >= MutationGrade.Quarantine ? 6f : 0f;
    }

    private static bool CanApplyTo(
        AttackContext context,
        MutationType mutationType,
        MutationTargetScope fallbackScope)
    {
        if (context.Mutations != null &&
            context.Mutations.TryGetDefinition(mutationType, out var definition))
            return definition.CanApplyTo(context.RangeType);

        return fallbackScope == MutationTargetScope.Common ||
               fallbackScope == MutationTargetScope.RangedOnly && context.IsRanged ||
               fallbackScope == MutationTargetScope.MeleeOnly && context.IsMelee;
    }
}
