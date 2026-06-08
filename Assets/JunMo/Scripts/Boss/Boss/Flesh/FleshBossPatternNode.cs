using UnityEngine;
using BossSystem.BehaviorTree;
using BossSystem;
using BossSystem.Scripable;

namespace BossSystem.Boss.FleshBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  패턴 1 : 돌진
    //  수정: rb.linearVelocity로 방향/속도 고정
    //        (rb.MovePosition은 FixedUpdate 타이밍 불일치로 방향 오류 발생)
    // ═══════════════════════════════════════════════════════════════
    public class ChargeNode : BTNode
    {
        private FleshBossController boss;
        private float chargeSpeed;
        private float chargeDuration;
        private float trailInterval;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Charge }
        private Phase   phase         = Phase.Idle;
        private float   chargeEndTime = 0f;
        private Vector2 chargeDir;
        private float   lastTrailTime = 0f;
        private Rigidbody2D rb;

        private float ChargeDistance => chargeSpeed * chargeDuration;

        public ChargeNode(BossBlackboard bb, FleshBossController boss,
            float chargeSpeed = 16f, float chargeDuration = 1.2f,
            float collisionDamage = 40f, float trailInterval = 0.15f,
            BossAttackData data = null) : base(bb)
        {
            this.boss           = boss;
            this.chargeSpeed    = chargeSpeed;
            this.chargeDuration = chargeDuration;
            this.trailInterval  = trailInterval;
            this.attackData     = data;
        }

        public override void OnEnter() => phase = Phase.Idle;

        protected override NodeState OnEvaluate()
        {
            switch (phase)
            {
                case Phase.Idle:
                    if (boss.IsExecutingPattern) return NodeState.Failure;
                    if (blackboard.PlayerTransform == null) return NodeState.Failure;

                    if (rb == null) rb = boss.GetRigidbody();
                    if (rb == null) return NodeState.Failure;

                    // ★ 텔레그래프 시작 시점의 플레이어 방향을 고정
                    chargeDir = new Vector2(
                        blackboard.DirectionToPlayer.x,
                        blackboard.DirectionToPlayer.y).normalized;

                    phase = Phase.Telegraph;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);

                    TelegraphHelper.SpawnLine(
                        boss.transform.position, chargeDir,
                        length: ChargeDistance, width: 2f,
                        attackData, onComplete: OnTelegraphDone);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Charge:
                    // 장판 생성
                    if (Time.time - lastTrailTime >= trailInterval)
                    {
                        lastTrailTime = Time.time;
                        boss.SpawnTrailZone(boss.transform.position);
                    }

                    // 돌진 종료
                    if (Time.time >= chargeEndTime)
                    {
                        // ★ 속도 정지
                        rb.linearVelocity = Vector2.zero;

                        boss.SetCharging(false);
                        boss.SetExecutingPattern(false);

                        if (blackboard.IsPhase2)
                            boss.SpawnFleshChunksAt(boss.transform.position, 3);

                        phase = Phase.Idle;
                        return NodeState.Success;
                    }
                    return NodeState.Running;
            }
            boss.ForceReleasePattern();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            phase = Phase.Idle;
            return NodeState.Failure;
        }

        private void OnTelegraphDone()
        {
            phase         = Phase.Charge;
            chargeEndTime = Time.time + chargeDuration;
            lastTrailTime = Time.time;
            boss.SetTelegraphing(false);
            boss.SetCharging(true);

            // ★ 속도 고정 — FixedUpdate/Update 타이밍 상관없이 일정한 직진
            rb.linearVelocity = chargeDir * chargeSpeed;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 2 : 살점 흩뿌리기 (변경 없음)
    // ═══════════════════════════════════════════════════════════════
    public class FleshScatterNode : BTNode
    {
        private FleshBossController boss;
        private int   scatterCount;
        private float force;
        private float fireInterval;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Attack }
        private Phase phase    = Phase.Idle;
        private int   fired    = 0;
        private float nextFire = 0f;

        public FleshScatterNode(BossBlackboard bb, FleshBossController boss,
            int scatterCount = 16, float force = 10f, float fireInterval = 0.05f,
            BossAttackData data = null) : base(bb)
        {
            this.boss         = boss;
            this.scatterCount = scatterCount;
            this.force        = force;
            this.fireInterval = fireInterval;
            this.attackData   = data;
        }

        public override void OnEnter() { phase = Phase.Idle; fired = 0; }

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
                        TelegraphShape.Circle, radius: force * 0.8f,
                        followParent: true, onComplete: OnTelegraphDone);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Attack:
                    if (Time.time >= nextFire && fired < scatterCount)
                    {
                        float   angle = (360f / scatterCount) * fired;
                        Vector3 dir   = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
                        dir.z = 0f;
                        boss.SpawnFleshProjectile(boss.transform.position,
                                                  dir.normalized, force, bounces: 0);
                        fired++;
                        nextFire = Time.time + fireInterval;
                    }

                    if (fired >= scatterCount)
                    {
                        boss.SetExecutingPattern(false);
                        if (blackboard.IsPhase2)
                            boss.SpawnFleshChunksAt(boss.transform.position, 4);
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
            phase    = Phase.Attack;
            fired    = 0;
            nextFire = Time.time;
            boss.SetTelegraphing(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 3 : 살점 던지기
    //  수정: 발사 방향에 위쪽 성분 추가 → 포물선 느낌
    //        플레이어 위치 고정(텔레그래프) + 살짝 위로 던지기
    // ═══════════════════════════════════════════════════════════════
    public class FleshThrowNode : BTNode
    {
        private FleshBossController boss;
        private int   bounceCount;
        private float throwForce;
        private int   throwCount;
        private float arcFactor;   // 포물선 위쪽 성분 (0=직선, 1=45도 위)
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Attack }
        private Phase   phase     = Phase.Idle;
        private int     thrown    = 0;
        private float   nextThrow = 0f;
        private Vector3 targetPos;

        public FleshThrowNode(BossBlackboard bb, FleshBossController boss,
            int bounceCount = 3, float throwForce = 14f, int throwCount = 1,
            float arcFactor = 0.4f, BossAttackData data = null) : base(bb)
        {
            this.boss        = boss;
            this.bounceCount = bounceCount;
            this.throwForce  = throwForce;
            this.throwCount  = throwCount;
            this.arcFactor   = arcFactor;
            this.attackData  = data;
        }

        public override void OnEnter() { phase = Phase.Idle; thrown = 0; }

        protected override NodeState OnEvaluate()
        {
            switch (phase)
            {
                case Phase.Idle:
                    if (boss.IsExecutingPattern) return NodeState.Failure;
                    if (blackboard.PlayerTransform == null) return NodeState.Failure;

                    // 텔레그래프 시점의 플레이어 위치 고정
                    targetPos = blackboard.PlayerTransform.position;

                    phase = Phase.Telegraph;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);

                    // 착탄 예정 위치에 원형 텔레그래프
                    TelegraphHelper.SpawnAt(targetPos, attackData,
                        TelegraphShape.Circle, radius: 1.5f,
                        onComplete: OnTelegraphDone);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Attack:
                    if (Time.time >= nextThrow && thrown < throwCount)
                    {
                        ThrowChunk();
                        thrown++;
                        nextThrow = Time.time + 0.3f;
                    }

                    if (thrown >= throwCount)
                    {
                        boss.SetExecutingPattern(false);
                        if (blackboard.IsPhase2)
                            boss.SpawnFleshChunksAt(boss.transform.position, 2);
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
            phase     = Phase.Attack;
            thrown    = 0;
            nextThrow = Time.time;
            boss.SetTelegraphing(false);
        }

        private void ThrowChunk()
        {
            // 플레이어 방향 벡터
            Vector3 toTarget = (targetPos - boss.transform.position);
            toTarget.z = 0f;
            Vector3 flatDir = toTarget.normalized;

            // ★ 위쪽 성분(Y+) 추가 → 포물선 던지기 느낌
            //   탑다운 2D에서 Y+ = 화면 위쪽 = 던지는 호의 정점 방향
            Vector3 launchDir = (flatDir + Vector3.up * arcFactor).normalized;

            boss.SpawnFleshProjectile(
                boss.transform.position,
                launchDir,
                throwForce,
                bounces: bounceCount,
                isLarge: true);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 4 : 근접 주먹 (변경 없음)
    // ═══════════════════════════════════════════════════════════════
    public class MeleeSmashNode : BTNode
    {
        private FleshBossController boss;
        private float smashRange;
        private float smashDamage;
        private float smashRadius;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Windup, Hit }
        private Phase phase      = Phase.Idle;
        private float windupEnd  = 0f;
        private float hitEnd     = 0f;
        private bool  hitApplied = false;

        private const float WINDUP_TIME = 0.3f;
        private const float HIT_TIME    = 0.2f;

        public MeleeSmashNode(BossBlackboard bb, FleshBossController boss,
            float smashRange = 4f, float smashDamage = 55f, float smashRadius = 3f,
            BossAttackData data = null) : base(bb)
        {
            this.boss        = boss;
            this.smashRange  = smashRange;
            this.smashDamage = smashDamage;
            this.smashRadius = smashRadius;
            this.attackData  = data;
        }

        public override void OnEnter() { phase = Phase.Idle; hitApplied = false; }

        protected override NodeState OnEvaluate()
        {
            switch (phase)
            {
                case Phase.Idle:
                    if (boss.IsExecutingPattern) return NodeState.Failure;
                    if (blackboard.DistanceToPlayer > smashRange) return NodeState.Failure;

                    phase = Phase.Telegraph;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);

                    TelegraphHelper.Spawn(boss.transform, attackData,
                        TelegraphShape.Circle, radius: smashRadius,
                        followParent: true, onComplete: OnTelegraphDone);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Windup:
                    if (Time.time >= windupEnd)
                    {
                        phase      = Phase.Hit;
                        hitEnd     = Time.time + HIT_TIME;
                        hitApplied = false;
                    }
                    return NodeState.Running;

                case Phase.Hit:
                    if (!hitApplied)
                    {
                        hitApplied = true;
                        boss.ApplySmashDamage(smashDamage, smashRadius);
                        boss.PlaySmashHit();
                    }
                    if (Time.time >= hitEnd)
                    {
                        boss.SetExecutingPattern(false);
                        if (blackboard.IsPhase2)
                            boss.SpawnFleshChunksAt(boss.transform.position, 3);
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
            phase     = Phase.Windup;
            windupEnd = Time.time + WINDUP_TIME;
            boss.PlaySmashWindup();
            boss.SetTelegraphing(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 5 : 살점 흡수 (변경 없음)
    // ═══════════════════════════════════════════════════════════════
    public class FleshAbsorbNode : BTNode
    {
        private FleshBossController boss;
        private float absorbRadius;
        private float healPerChunk;
        private float pullSpeed;
        private float pullDuration;

        private bool  isActive  = false;
        private float startTime = 0f;
        private bool  absorbed  = false;

        public FleshAbsorbNode(BossBlackboard bb, FleshBossController boss,
            float absorbRadius = 12f, float healPerChunk = 40f,
            float pullSpeed = 8f, float pullDuration = 1.5f) : base(bb)
        {
            this.boss         = boss;
            this.absorbRadius = absorbRadius;
            this.healPerChunk = healPerChunk;
            this.pullSpeed    = pullSpeed;
            this.pullDuration = pullDuration;
        }

        public override void OnEnter() { isActive = false; absorbed = false; }

        protected override NodeState OnEvaluate()
        {
            if (!blackboard.IsPhase2)                return NodeState.Failure;
            if (!boss.HasNearbyChunks(absorbRadius)) return NodeState.Failure;
            if (boss.IsExecutingPattern)             return NodeState.Failure;

            if (!isActive)
            {
                isActive  = true;
                absorbed  = false;
                startTime = Time.time;
                boss.StartAbsorbEffect();
                boss.SetExecutingPattern(true);
                boss.SetTelegraphing(true);
                return NodeState.Running;
            }

            boss.PullChunksToward(absorbRadius, pullSpeed);

            if (!absorbed && Time.time >= startTime + pullDuration)
            {
                absorbed = true;
                int count = boss.AbsorbNearbyChunks(absorbRadius * 0.8f);
                boss.Heal(count * healPerChunk);
                boss.StopAbsorbEffect();
                boss.SetTelegraphing(false);
                boss.SetExecutingPattern(false);
            }

            if (absorbed)
            {
                isActive = false;
                return NodeState.Success;
            }
            return NodeState.Running;
        }
    }
}