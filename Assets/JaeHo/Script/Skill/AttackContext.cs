using UnityEngine;

public enum AttackRangeType
{
    Ranged,
    Melee
}

public enum AttackShapeType
{
    Bullet,
    Fan,
    ForwardBox
}

/// <summary>
/// 한 번의 공격 시도에 대한 고정 정보.
/// 명중 여부와 무관하게 "어떤 공격이었는가"를 표현한다.
/// </summary>
public readonly struct AttackContext
{
    public readonly SkillBase SourceSkill;
    public readonly GameObject Attacker;
    public readonly MutationLoadout Mutations;
    public readonly AttackRangeType RangeType;
    public readonly AttackShapeType ShapeType;
    public readonly Vector2 Origin;
    public readonly Vector2 Direction;
    public readonly float BaseDamage;
    public readonly float CriticalChance;
    public readonly float CriticalMultiplier;
    public readonly SkillTag Tags;

    public bool IsRanged => RangeType == AttackRangeType.Ranged;
    public bool IsMelee => RangeType == AttackRangeType.Melee;

    public AttackContext(
        SkillBase sourceSkill,
        GameObject attacker,
        MutationLoadout mutations,
        AttackRangeType rangeType,
        AttackShapeType shapeType,
        Vector2 origin,
        Vector2 direction,
        float baseDamage,
        SkillTag tags,
        float criticalChance = 0f,
        float criticalMultiplier = 2f)
    {
        SourceSkill = sourceSkill;
        Attacker = attacker;
        Mutations = mutations;
        RangeType = rangeType;
        ShapeType = shapeType;
        Origin = origin;
        Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        BaseDamage = baseDamage;
        CriticalChance = Mathf.Clamp01(criticalChance);
        CriticalMultiplier = Mathf.Max(1f, criticalMultiplier);
        Tags = tags;
    }

    public bool HasTag(SkillTag tag)
    {
        return (Tags & tag) != 0;
    }

    public AttackContext WithOriginAndDirection(Vector2 origin, Vector2 direction)
    {
        return new AttackContext(SourceSkill, Attacker, Mutations, RangeType, ShapeType, origin,
            direction, BaseDamage, Tags, CriticalChance, CriticalMultiplier);
    }

    public AttackContext WithDamageMultiplier(float multiplier)
    {
        return new AttackContext(SourceSkill, Attacker, Mutations, RangeType, ShapeType, Origin,
            Direction, BaseDamage * Mathf.Max(0f, multiplier), Tags, CriticalChance, CriticalMultiplier);
    }
}
