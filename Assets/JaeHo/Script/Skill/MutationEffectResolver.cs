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
            SkillTag.None,
            MutationTargetScope.Common);

        if (grade == MutationGrade.None) return false;

        if (TryGetGradeData(context, MutationType.FollowUp, SkillTag.None,
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
