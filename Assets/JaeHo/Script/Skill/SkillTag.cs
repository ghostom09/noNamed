using System;

/// <summary>
/// 스킬(무기)에 동적으로 부착되는 특성 태그.
/// [Flags]를 사용해 비트 연산으로 여러 태그를 중첩 가능.
///
/// 사용 예시:
///   skill.AddTag(SkillTag.Piercing | SkillTag.Explosive);
///   skill.HasTag(SkillTag.Bounce);
/// </summary>
[Flags]
public enum SkillTag
{
    None        = 0,
    
    // ── 투사체 계열 (ProjectileSkill) ──────────────────────────
    
    /// <summary> 관통 - 적을 뚫고 지나감. SkillB 기본 내장. </summary>
    Piercing    = 1 << 0,   // 1
    
    /// <summary> 바운스 - 벽(지형)에 튕김. 최대 횟수는 BulletData에서 관리. </summary>
    Bounce      = 1 << 1,   // 2
    
    /// <summary> 폭발 - 착탄 지점 범위 데미지. </summary>
    Explosive   = 1 << 2,   // 4
    
    // ── 근거리 계열 (MeleeSkill) ───────────────────────────────
    
    /// <summary> 범위 확장 - 부채꼴/박스 범위를 넓힘. </summary>
    WidenRange  = 1 << 3,   // 8
    
    /// <summary> 다단 히트 - 한 번의 스윙에 히트 판정을 여러 번 냄. </summary>
    MultiHit    = 1 << 4,   // 16
    
    // ── 공통 ───────────────────────────────────────────────────
    
    /// <summary> 독 - 타격 후 DoT 데미지 적용. </summary>
    Poison      = 1 << 5,   // 32
    
    /// <summary> 둔화 - 타격 대상 이동속도 감소. </summary>
    Slow        = 1 << 6,   // 64

    /// <summary> Follow-up hit - applies an extra hit based on the previous damage. </summary>
    FollowUp    = 1 << 7,   // 128
}
