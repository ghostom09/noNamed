using UnityEngine;

/// <summary>
/// SkillC - 부채꼴 근거리 공격.
/// 마우스 방향 중심으로 halfAngle 범위 안의 적에게 데미지.
/// 공격 시 ParticleSystem으로 이펙트 재생.
/// </summary>
public class SkillC : MeleeSkill
{
    [Header("--- Fan Attack Settings ---")]
    [Tooltip("부채꼴의 절반 각도. 60 = 총 120도 범위")]
    [SerializeField] private float halfAngle = 60f;

    [Header("--- Effect ---")]
    [Tooltip("부채꼴 범위를 표현할 ParticleSystem. firePoint 자식에 배치 권장")]
    [SerializeField] private ParticleSystem slashEffect;

    protected override void Awake()
    {
        base.Awake();

        if (slashEffect == null)
            Debug.LogWarning($"[{name}] slashEffect가 할당되지 않음 - 이펙트 없이 동작");
    }

    public override void OnAttack(SkillInputState input)
    {
        AttackTimer += Time.deltaTime;

        if (!input.AttackPressed) return;
        if (AttackTimer < attackCooldown) return;

        AttackTimer = 0f;
        ExecuteAttack();
    }

    private void ExecuteAttack()
    {
        if (firePoint == null) return;

        Vector2 origin = firePoint.position;

        // FirePointRotator가 회전시켜둔 방향 사용
        Vector2 forward = firePoint.right;
        AttackContext context = CreateAttackContext(
            AttackRangeType.Melee,
            AttackShapeType.Fan,
            origin,
            forward);

        float radius = HasTag(SkillTag.WidenRange) ? attackRadius * 1.5f : attackRadius;

        // 이펙트 재생
        PlayEffect(forward);
        AbsorbProjectilesInRadius(context, origin, radius);

        int hitCount = CollectTargetsInRadius(origin, radius);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = GetCollectedTarget(i);
            if (hit == null) continue;
            if (!IsInFOV(origin, hit.bounds.center, forward, halfAngle)) continue;
            if (!HasLineOfSight(origin, hit)) continue;

            ApplyDamage(context, hit, hit.bounds.center);
        }
    }

    private void PlayEffect(Vector2 forward)
    {
        if (slashEffect == null) return;

        // firePoint가 이미 마우스 방향을 향하고 있으므로 그대로 재생
        slashEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        slashEffect.Play();
    }
}
