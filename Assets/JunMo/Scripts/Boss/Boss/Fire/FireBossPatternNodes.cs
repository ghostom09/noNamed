using UnityEngine;
using BossSystem.BehaviorTree;
using BossSystem.Boss;
using BossSystem;
using BossSystem.Scripable;

namespace BossSystem.Boss.FireBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  공통 규칙 (모든 패턴 노드)
    //
    //  [시작] boss.IsExecutingPattern 체크 → 다른 패턴 실행 중이면 Failure
    //  [시작] SetExecutingPattern(true) + SetTelegraphing(true)
    //  [종료] SetExecutingPattern(false) — Success/Failure 모든 경로에서
    //  [실패 경로] ForceReleasePattern() 으로 플래그 보장 해제
    // ═══════════════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════════════
    //  패턴 1 : 근접 화염 방사
    //  텔레그래프: 부채꼴 — 반지름=closeRange, 각도=fanAngle
    // ═══════════════════════════════════════════════════════════════
    public class FlameBreathNode : BTNode
    {
        private FireBossController boss;
        private float closeRange;
        private float fanAngle;
        private float damagePerSec;
        private float duration;
        private float tickInterval;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Attack }
        private Phase phase       = Phase.Idle;
        private float attackStart = 0f;
        private float lastTick    = 0f;

        public FlameBreathNode(BossBlackboard bb, FireBossController boss,
            float closeRange = 5f, float fanAngle = 90f,
            float damagePerSec = 30f, float duration = 2.5f,
            BossAttackData data = null) : base(bb)
        {
            this.boss         = boss;
            this.closeRange   = closeRange;
            this.fanAngle     = fanAngle;
            this.damagePerSec = damagePerSec;
            this.duration     = duration;
            this.tickInterval = 0.2f;
            this.attackData   = data;
        }

        protected override NodeState OnEvaluate()
        {
            switch (phase)
            {
                case Phase.Idle:
                    if (boss.IsExecutingPattern)        return NodeState.Failure;
                    if (blackboard.DistanceToPlayer > closeRange) return NodeState.Failure;

                    phase = Phase.Telegraph;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);

                    // 부채꼴: direction = 정규화방향 * fanAngle(도) → magnitude=각도로 전달
                    Vector2 dir2D = new Vector2(
                        blackboard.DirectionToPlayer.x,
                        blackboard.DirectionToPlayer.y).normalized * fanAngle;

                    TelegraphHelper.Spawn(boss.transform, attackData,
                        TelegraphShape.Sector,
                        radius: closeRange,
                        direction: dir2D,
                        followParent: true,
                        onComplete: OnTelegraphDone);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Attack:
                    if (Time.time - lastTick >= tickInterval)
                    {
                        lastTick = Time.time;
                        ApplyFanDamage();
                    }

                    if (Time.time - attackStart >= duration)
                    {
                        boss.PlayFlameBreathVFX(false);
                        boss.SetExecutingPattern(false);

                        if (blackboard.IsPhase2)
                            boss.SpawnGasCloud(boss.transform.position, fanAngle);

                        phase = Phase.Idle;
                        return NodeState.Success;
                    }
                    return NodeState.Running;
            }
            // 도달 불가 경로 — 안전 해제
            boss.ForceReleasePattern();
            phase = Phase.Idle;
            return NodeState.Failure;
        }

        private void OnTelegraphDone()
        {
            phase       = Phase.Attack;
            attackStart = Time.time;
            lastTick    = Time.time;
            boss.SetTelegraphing(false);
            boss.PlayFlameBreathVFX(true);
        }

        private void ApplyFanDamage()
        {
            var player = blackboard.PlayerTransform;
            if (player == null) return;
            Vector3 toPlayer = player.position - boss.transform.position;
            float angle = Vector2.Angle(boss.transform.up,
                                        new Vector2(toPlayer.x, toPlayer.y));
            if (angle <= fanAngle * 0.5f && toPlayer.magnitude <= closeRange)
            {
                // player.GetComponent<PlayerHealth>()?.TakeDamage(damagePerSec * tickInterval);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 2 : 화염탄 난사
    //  텔레그래프: 원형(보스 중심) 충전 암시
    //  가스는 FireballProjectile 착탄 시 스폰 (패턴 노드 종료 후 X)
    // ═══════════════════════════════════════════════════════════════
    public class FireballBarrageNode : BTNode
    {
        private FireBossController boss;
        private float farRange;
        private int   shotCount;
        private float spreadAngle;
        private float fireInterval;
        private float projectileSpeed;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Attack }
        private Phase phase      = Phase.Idle;
        private int   shotsFired = 0;
        private float nextFire   = 0f;

        public FireballBarrageNode(BossBlackboard bb, FireBossController boss,
            float farRange = 8f, int shotCount = 8, float spreadAngle = 30f,
            float fireInterval = 0.15f, float projectileSpeed = 12f,
            BossAttackData data = null) : base(bb)
        {
            this.boss            = boss;
            this.farRange        = farRange;
            this.shotCount       = shotCount;
            this.spreadAngle     = spreadAngle;
            this.fireInterval    = fireInterval;
            this.projectileSpeed = projectileSpeed;
            this.attackData      = data;
        }

        protected override NodeState OnEvaluate()
        {
            switch (phase)
            {
                case Phase.Idle:
                    if (boss.IsExecutingPattern) return NodeState.Failure;
                    if (blackboard.DistanceToPlayer < farRange) return NodeState.Failure;

                    phase = Phase.Telegraph;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);

                    TelegraphHelper.Spawn(boss.transform, attackData,
                        TelegraphShape.Circle, radius: 2f,
                        followParent: true, onComplete: OnTelegraphDone);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Attack:
                    if (Time.time >= nextFire && shotsFired < shotCount)
                    {
                        FireOne();
                        shotsFired++;
                        nextFire = Time.time + fireInterval;
                    }

                    if (shotsFired >= shotCount)
                    {
                        boss.SetExecutingPattern(false);
                        phase = Phase.Idle;
                        return NodeState.Success;
                    }
                    return NodeState.Running;
            }
            boss.ForceReleasePattern();
            phase = Phase.Idle;
            return NodeState.Failure;
        }

        private void OnTelegraphDone()
        {
            phase      = Phase.Attack;
            shotsFired = 0;
            nextFire   = Time.time;
            boss.SetTelegraphing(false);
        }

        private void FireOne()
        {
            var player = blackboard.PlayerTransform;
            if (player == null) return;
            Vector3 baseDir = (player.position - boss.transform.position).normalized;
            float   rndAng  = Random.Range(-spreadAngle, spreadAngle);
            Vector3 dir     = Quaternion.Euler(0, 0, rndAng) * baseDir;
            dir.z = 0f;
            boss.SpawnFireball(boss.transform.position, dir, projectileSpeed,
                               blackboard.IsPhase2);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 3 : 확산하는 불의 고리
    //  텔레그래프: 원형(보스 중심), 반지름=maxRadius
    // ═══════════════════════════════════════════════════════════════
    public class ExpandingFireRingNode : BTNode
    {
        private FireBossController boss;
        private int   ringCount;
        private float ringInterval;
        private float expandSpeed;
        private float maxRadius;
        private float damage;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Attack }
        private Phase phase        = Phase.Idle;
        private int   ringsSpawned = 0;
        private float nextRingTime = 0f;

        public ExpandingFireRingNode(BossBlackboard bb, FireBossController boss,
            int ringCount = 3, float ringInterval = 0.8f,
            float expandSpeed = 6f, float maxRadius = 12f, float damage = 40f,
            BossAttackData data = null) : base(bb)
        {
            this.boss         = boss;
            this.ringCount    = ringCount;
            this.ringInterval = ringInterval;
            this.expandSpeed  = expandSpeed;
            this.maxRadius    = maxRadius;
            this.damage       = damage;
            this.attackData   = data;
        }

        protected override NodeState OnEvaluate()
        {
            switch (phase)
            {
                case Phase.Idle:
                    if (boss.IsExecutingPattern) return NodeState.Failure;

                    phase = Phase.Telegraph;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);

                    TelegraphHelper.Spawn(boss.transform, attackData,
                        TelegraphShape.Circle, radius: maxRadius,
                        followParent: true, onComplete: OnTelegraphDone);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Attack:
                    if (Time.time >= nextRingTime && ringsSpawned < ringCount)
                    {
                        boss.SpawnFireRing(boss.transform.position, expandSpeed,
                                           maxRadius, damage, ringsSpawned * 0.05f,
                                           blackboard.IsPhase2);
                        ringsSpawned++;
                        nextRingTime = Time.time + ringInterval;
                    }

                    if (ringsSpawned >= ringCount &&
                        Time.time >= nextRingTime + maxRadius / expandSpeed)
                    {
                        boss.SetExecutingPattern(false);

                        if (blackboard.IsPhase2)
                            boss.SpawnGasCloud(boss.transform.position, 360f);

                        phase = Phase.Idle;
                        return NodeState.Success;
                    }
                    return NodeState.Running;
            }
            boss.ForceReleasePattern();
            phase = Phase.Idle;
            return NodeState.Failure;
        }

        private void OnTelegraphDone()
        {
            phase        = Phase.Attack;
            ringsSpawned = 0;
            nextRingTime = Time.time;
            boss.SetTelegraphing(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  페이즈2 가스 도트 데미지 (Parallel 상시 실행)
    // ═══════════════════════════════════════════════════════════════
    public class GasDotDamageNode : BTNode
    {
        private FireBossController boss;
        private float damagePerSec;
        private float tickInterval;
        private float lastTick;

        public GasDotDamageNode(BossBlackboard bb, FireBossController boss,
            float damagePerSec = 15f) : base(bb)
        {
            this.boss         = boss;
            this.damagePerSec = damagePerSec;
            this.tickInterval = 0.5f;
        }

        protected override NodeState OnEvaluate()
        {
            if (!blackboard.IsPhase2) return NodeState.Failure;
            if (Time.time - lastTick >= tickInterval)
            {
                lastTick = Time.time;
                boss.ApplyGasDotToPlayer(damagePerSec * tickInterval);
            }
            return NodeState.Running;
        }
    }
}