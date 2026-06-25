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
        private float telegraphDelay;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Attack }
        private Phase _phase       = Phase.Idle;
        private float _attackStart = 0f;
        private float _lastTick    = 0f;
        private Vector2 _attackDirection = Vector2.up;
        public FlameBreathNode(BossBlackboard bb, FireBossController boss,
            float closeRange = 5f, float fanAngle = 90f,
            float damagePerSec = 30f, float duration = 2.5f,
            float telegraphDelay = 0.3f,
            BossAttackData data = null) : base(bb)
        {
            this.boss         = boss;
            this.closeRange   = closeRange;
            this.fanAngle     = fanAngle;
            this.damagePerSec = damagePerSec;
            this.duration     = duration;
            this.tickInterval = 0.2f;
            this.telegraphDelay = telegraphDelay;
            this.attackData   = data;
        }

        protected override NodeState OnEvaluate()
        {
            switch (_phase)
            {
                case Phase.Idle:
                    if (boss.IsExecutingPattern)        return NodeState.Failure;
                    if (blackboard.DistanceToPlayer > closeRange) return NodeState.Failure;

                    _phase = Phase.Telegraph;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);

                    // Lock the attack direction so the telegraph and damage area stay aligned.
                    _attackDirection = new Vector2(
                        blackboard.DirectionToPlayer.x,
                        blackboard.DirectionToPlayer.y).normalized;

                    boss.SpawnTelegraph(attackData,
                        TelegraphShape.Sector,
                        radius: closeRange,
                        direction: _attackDirection,
                        followBoss: true,
                        onComplete: OnTelegraphDone,
                        duration: telegraphDelay);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Attack:
                    if (Time.time - _lastTick >= tickInterval)
                    {
                        _lastTick = Time.time;
                        ApplyFanDamage();
                    }

                    if (Time.time - _attackStart >= duration)
                    {
                        boss.PlayFlameBreathVFX(false);
                        boss.SetExecutingPattern(false);

                        if (blackboard.IsPhase2)
                            boss.SpawnGasCloud(boss.transform.position, fanAngle);

                        _phase = Phase.Idle;
                        return NodeState.Success;
                    }
                    return NodeState.Running;
            }
            // 도달 불가 경로 — 안전 해제
            boss.ForceReleasePattern();
            _phase = Phase.Idle;
            return NodeState.Failure;
        }

        private void OnTelegraphDone()
        {
            _phase       = Phase.Attack;
            _attackStart = Time.time;
            _lastTick    = Time.time;
            boss.SetTelegraphing(false);
            boss.PlayFlameBreathVFX(true);
            ApplyFanDamage();
        }

        private void ApplyFanDamage()
        {
            var player = blackboard.PlayerTransform;
            if (player == null) return;

            Vector2 toPlayer = (Vector2)(player.position - boss.transform.position);
            float angle = Vector2.Angle(_attackDirection, toPlayer);
            if (angle <= fanAngle * 0.5f && toPlayer.magnitude <= closeRange)
            {
                FireBossController.ApplyDamageToPlayer(player.gameObject, damagePerSec * tickInterval);
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

                    boss.SpawnTelegraph(attackData,
                        TelegraphShape.Circle, radius: 2f,
                        followBoss: true, onComplete: OnTelegraphDone);

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

                        if (blackboard.IsPhase2)
                            boss.SpawnGasCloud(boss.transform.position, spreadAngle);

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
        private float ringWidth;
        private float telegraphDelay;
        private float damage;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Attack, WaitNextTelegraph }
        private Phase phase        = Phase.Idle;
        private int   ringsSpawned = 0;
        private float nextRingTime = 0f;

        public ExpandingFireRingNode(BossBlackboard bb, FireBossController boss,
            int ringCount = 3, float ringInterval = 0.3f,
            float expandSpeed = 6f, float maxRadius = 12f, float damage = 40f,
            float ringWidth = 1.5f, float telegraphDelay = 0.5f,
            BossAttackData data = null) : base(bb)
        {
            this.boss         = boss;
            this.ringCount    = ringCount;
            this.ringInterval = ringInterval;
            this.expandSpeed  = expandSpeed;
            this.maxRadius    = maxRadius;
            this.ringWidth    = ringWidth;
            this.telegraphDelay = telegraphDelay;
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
                    ringsSpawned = 0;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);

                    SpawnCurrentTelegraph();

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Attack:
                    SpawnCurrentRing();
                    ringsSpawned++;
                    nextRingTime = Time.time + ringInterval;

                    if (ringsSpawned >= ringCount)
                    {
                        if (Time.time >= nextRingTime)
                            return FinishPattern();

                        phase = Phase.WaitNextTelegraph;
                        return NodeState.Running;
                    }

                    phase = Phase.WaitNextTelegraph;
                    return NodeState.Running;

                case Phase.WaitNextTelegraph:
                    if (Time.time < nextRingTime)
                        return NodeState.Running;

                    if (ringsSpawned >= ringCount)
                        return FinishPattern();

                    phase = Phase.Telegraph;
                    boss.SetTelegraphing(true);
                    SpawnCurrentTelegraph();
                    return NodeState.Running;
            }
            boss.ForceReleasePattern();
            phase = Phase.Idle;
            return NodeState.Failure;
        }

        private void OnTelegraphDone()
        {
            phase = Phase.Attack;
            boss.SetTelegraphing(false);
        }

        private void SpawnCurrentTelegraph()
        {
            GetCurrentRingRadii(out float innerRadius, out float outerRadius, out bool isCenterAttack);
            boss.SpawnFireRingTelegraph(attackData, innerRadius, outerRadius, isCenterAttack,
                onComplete: OnTelegraphDone, duration: telegraphDelay);
        }

        private void SpawnCurrentRing()
        {
            GetCurrentRingRadii(out float innerRadius, out float outerRadius, out bool isCenterAttack);
            boss.SpawnFireRing(boss.transform.position, expandSpeed,
                               outerRadius, damage, 0f,
                               blackboard.IsPhase2,
                               useCenterPrefab: isCenterAttack,
                               innerRadius: innerRadius);
        }

        private void GetCurrentRingRadii(out float innerRadius, out float outerRadius, out bool isCenterAttack)
        {
            float centerRadius = boss.GetFireRingCenterRadius();
            innerRadius = 0f;
            outerRadius = centerRadius;
            isCenterAttack = ringsSpawned == 0;

            if (!isCenterAttack)
            {
                innerRadius = centerRadius + ringWidth * (ringsSpawned - 1);
                outerRadius = ringsSpawned == ringCount - 1
                    ? maxRadius
                    : centerRadius + ringWidth * ringsSpawned;
            }
        }

        private NodeState FinishPattern()
        {
            boss.SetExecutingPattern(false);

            if (blackboard.IsPhase2)
                boss.SpawnGasCloud(boss.transform.position, 360f);

            phase = Phase.Idle;
            return NodeState.Success;
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
