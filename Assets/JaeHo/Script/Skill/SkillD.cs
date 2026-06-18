using UnityEngine;

/// <summary>
/// SkillD - 전방 직선 근거리 공격.
/// 마우스 방향으로 BoxCast. 공격 시 ParticleSystem으로 이펙트 재생.
/// </summary>
public class SkillD : MeleeSkill
{
    [Header("--- Box Attack Settings ---")]
    [Tooltip("박스의 너비")]
    [SerializeField] private float boxWidth = 1.5f;

    [Header("--- Effect ---")]
    [Tooltip("전방 타격을 표현할 ParticleSystem. firePoint 자식에 배치 권장")]
    [SerializeField] private ParticleSystem strikeEffect;

    protected override void Awake()
    {
        base.Awake();

        if (strikeEffect == null)
            Debug.LogWarning($"[{name}] strikeEffect가 할당되지 않음 - 이펙트 없이 동작");
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
        Vector2 direction = firePoint.right;
        AttackContext context = CreateAttackContext(
            AttackRangeType.Melee,
            AttackShapeType.ForwardBox,
            origin,
            direction);

        float width = HasTag(SkillTag.WidenRange) ? boxWidth * 1.5f : boxWidth;
        Vector2 boxSize = new Vector2(attackDistance, width);

        // 이펙트 재생
        PlayEffect();
        AbsorbProjectilesInOrientedBox(context, origin, direction, boxSize);

        int hitCount = CollectTargetsInOrientedBox(origin, direction, boxSize);
        SortCollectedTargetsByDistance(origin, hitCount);

        if (HasTag(SkillTag.MultiHit))
        {
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = GetCollectedTarget(i);
                if (hit == null) continue;
                if (!HasLineOfSight(origin, hit)) continue;

                ApplyDamage(context, hit, hit.bounds.center);
            }
        }
        else
        {
            bool normalTargetHit = false;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = GetCollectedTarget(i);
                if (hit == null) continue;
                if (!HasLineOfSight(origin, hit)) continue;

                bool isFleshChunk = hit.GetComponentInParent<BossSystem.Boss.FleshBoss.FleshChunk>() != null;

                if (!isFleshChunk && normalTargetHit) continue;

                ApplyDamage(context, hit, hit.bounds.center);

                if (!isFleshChunk)
                    normalTargetHit = true;
            }
        }
    }

    private void PlayEffect()
    {
        if (strikeEffect == null) return;

        strikeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        strikeEffect.Play();
    }
}
