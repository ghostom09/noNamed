public enum MutationType
{
    Ricochet,
    FollowUp,
    Spread,
    Pierce,
    Homing,
    ProjectileAbsorb,
    Knockback,
    Stun,
    Slow,
    Bind,
    Explosion,
    Poison
}

public enum MutationGrade
{
    None = 0,
    Safe = 1,
    Caution = 2,
    Danger = 3,
    Quarantine = 4
}

public enum MutationTargetScope
{
    Common,
    RangedOnly,
    MeleeOnly
}

public enum MutationTriggerType
{
    Passive,
    OnAttack,
    OnHit,
    OnCriticalHit,
    OnKill,
    OnProjectileExpired
}
