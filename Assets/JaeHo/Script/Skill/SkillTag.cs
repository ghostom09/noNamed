using System;

[Flags]
public enum SkillTag
{
    None = 0,

    // Projectile tags
    Piercing = 1 << 0,
    Bounce = 1 << 1,
    Explosive = 1 << 2,

    // Melee tags
    WidenRange = 1 << 3,
    MultiHit = 1 << 4,

    // Shared mutation/debug tags
    Poison = 1 << 5,
    Slow = 1 << 6,
    FollowUp = 1 << 7,
    Spread = 1 << 8,
    Homing = 1 << 9,
    Stun = 1 << 10,
    Bind = 1 << 11,
    Knockback = 1 << 12,
    ProjectileAbsorb = 1 << 13
}
