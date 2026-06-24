using UnityEngine;
using BossSystem.BehaviorTree;
using BossSystem;
using BossSystem.Scripable;

namespace BossSystem.Boss.FleshBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  패턴 1 : 돌진
    // ═══════════════════════════════════════════════════════════════
    public class ChargeNode : BTNode
    {
        private FleshBossController boss;
        private float chargeSpeed;
        private float collisionDamage;
        private float trailInterval;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Charge }
        private Phase phase         = Phase.Idle;
        private float chargeEndTime = 0f;
        private Vector2 chargeDir;          // 완전히 고정된 2D 방향
        private Vector2 chargeStartPos;
        private Vector2 chargeTargetPos;
        private float chargeDistance;
        private float chargeElapsed;
        private float lastTrailTime = 0f;
        private bool hasLoggedFirstMove = false;
        private bool hasExpectedPosition = false;
        private Vector2 lastExpectedPosition;
        private RigidbodyType2D originalBodyType;
        private float originalGravityScale;
        private bool hasSavedPhysicsState = false;
        private Rigidbody2D rb;

        private const float TargetReachThreshold = 0.05f;
        private const float ChargeTimeoutPadding = 0.2f;
        
        public ChargeNode(BossBlackboard bb, FleshBossController boss,
            float chargeSpeed = 16f, float chargeDuration = 1.2f,
            float collisionDamage = 40f, float trailInterval = 0.02f,
            BossAttackData data = null) : base(bb)
        {
            this.boss           = boss;
            this.chargeSpeed    = chargeSpeed;
            this.collisionDamage = collisionDamage;
            this.trailInterval  = trailInterval;
            this.attackData     = data;
        }

        public override void OnEnter()
        {
            EndChargePhysics();
            phase = Phase.Idle;
            hasExpectedPosition = false;
            if (rb) rb.linearVelocity = Vector2.zero;
        }

        protected override NodeState OnEvaluate()
        {
            switch (phase)
            {
                case Phase.Idle:
                {
                    if (boss.IsExecutingPattern) return NodeState.Failure;

                    var player = blackboard.PlayerTransform;
                    if (player == null) return NodeState.Failure;

                    if (rb == null) rb = boss.GetRigidbody();
                    if (rb == null) return NodeState.Failure;

                    Vector2 bossPos   = rb.transform.position;
                    Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
                    Vector2 rawDir    = playerPos - bossPos;

                    if (rawDir.sqrMagnitude < 0.5f) return NodeState.Failure;

                    chargeStartPos = bossPos;
                    chargeTargetPos = playerPos;
                    chargeDir = rawDir.normalized;
                    chargeDistance = rawDir.magnitude;
                    hasLoggedFirstMove = false;
                    hasExpectedPosition = false;

                    phase = Phase.Telegraph;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);

                    boss.SpawnLineTelegraph(
                        boss.transform.position - (Vector3)(chargeDir * (chargeDistance * 0.5f)),
                        chargeDir,
                        length: chargeDistance, width: 2f,
                        data: attackData, onComplete: OnTelegraphDone);

                    return NodeState.Running;
                }

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Charge:
                {
                    Vector2 current = rb.position;
                    if (hasExpectedPosition)
                    {
                        Vector2 drift = current - lastExpectedPosition;
                        if (drift.sqrMagnitude > 0.0004f)
                        {
                            Debug.LogWarning(
                                $"[FleshBoss][Charge][ExternalMoveDetected] state={phase} " +
                                $"expected={lastExpectedPosition} actual={current} drift={drift} " +
                                $"target={chargeTargetPos} dashDir={chargeDir} rbVelocity={rb.linearVelocity} " +
                                $"transformPos={boss.transform.position}");
                        }
                    }

                    chargeElapsed += Time.fixedDeltaTime;
                    float travelDistance = Mathf.Min(chargeElapsed * chargeSpeed, chargeDistance);
                    float remaining = Mathf.Max(0f, chargeDistance - travelDistance);
                    Vector2 actualMoveDir = chargeDir;
                    Vector2 next = remaining <= TargetReachThreshold
                        ? chargeTargetPos
                        : chargeStartPos + actualMoveDir * travelDistance;

                    rb.linearVelocity = Vector2.zero;
                    rb.MovePosition(next);
                    boss.ApplyChargeDamageAt(next, collisionDamage);
                    lastExpectedPosition = next;
                    hasExpectedPosition = true;

                    Debug.Log(
                        $"[FleshBoss][Charge][Frame] state={phase} bossPos={rb.position} " +
                        $"transformPos={boss.transform.position} target={chargeTargetPos} " +
                        $"dashDir={chargeDir} actualMoveDir={actualMoveDir} rbVelocity={rb.linearVelocity} " +
                        $"next={next} elapsed={chargeElapsed:F3} remaining={remaining:F3}");

                    if (!hasLoggedFirstMove)
                    {
                        hasLoggedFirstMove = true;
                        Debug.Log(
                            $"[FleshBoss][Charge][FirstMove] current={current} target={chargeTargetPos} " +
                            $"calculatedDir={chargeDir} actualMoveDir={actualMoveDir} next={next} " +
                            $"remaining={remaining:F3} rbVelocity={rb.linearVelocity}");
                    }

                    // 장판 생성
                    if (Time.time - lastTrailTime >= trailInterval)
                    {
                        lastTrailTime = Time.time;
                        boss.SpawnTrailZone(boss.transform.position);
                    }

                    if (Vector2.Distance(next, chargeTargetPos) <= TargetReachThreshold)
                    {
                        rb.MovePosition(chargeTargetPos);
                        rb.linearVelocity = Vector2.zero;
                        Debug.Log(
                            $"[FleshBoss][Charge][Arrived] boss={rb.position} target={chargeTargetPos} " +
                            $"calculatedDir={chargeDir} actualMoveDir={actualMoveDir}");
                        EndChargePhysics();
                        boss.SetCharging(false);
                        boss.SetExecutingPattern(false);

                        if (blackboard.IsPhase2)
                            boss.SpawnFleshChunksAt(boss.transform.position, 3);

                        phase = Phase.Idle;
                        return NodeState.Success;
                    }

                    // 돌진 종료
                    if (Time.time >= chargeEndTime) // ? 특정위치까지 간걸 확인후 scuccess 반환
                    {
                        rb.linearVelocity = Vector2.zero;
                        Debug.LogWarning(
                            $"[FleshBoss][Charge][Timeout] boss={rb.position} start={chargeStartPos} " +
                            $"target={chargeTargetPos} calculatedDir={chargeDir} actualMoveDir={actualMoveDir}");
                        EndChargePhysics();
                        boss.SetCharging(false);
                        boss.SetExecutingPattern(false);

                        if (blackboard.IsPhase2)
                            boss.SpawnFleshChunksAt(boss.transform.position, 3);

                        phase = Phase.Idle;
                        return NodeState.Success;
                    }
                    return NodeState.Running;
                }
            }

            boss.ForceReleasePattern();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            EndChargePhysics();
            phase = Phase.Idle;
            return NodeState.Failure;
        }

        private void OnTelegraphDone()
        {
            phase         = Phase.Charge;
            chargeEndTime = Time.time + (chargeDistance / chargeSpeed) + ChargeTimeoutPadding;
            chargeElapsed = 0f;
            lastTrailTime = Time.time;
            boss.SetTelegraphing(false);
            boss.SetCharging(true);
            BeginChargePhysics();
            Debug.Log(
                $"[FleshBoss][Charge][Start] boss={rb.position} start={chargeStartPos} " +
                $"target={chargeTargetPos} calculatedDir={chargeDir} actualMoveDir={chargeDir} " +
                $"distance={chargeDistance:F3}");
            // velocity는 설정하지 않음 — MovePosition으로만 이동
        }

        private void BeginChargePhysics()
        {
            if (rb == null) return;

            if (!hasSavedPhysicsState)
            {
                originalBodyType = rb.bodyType;
                originalGravityScale = rb.gravityScale;
                hasSavedPhysicsState = true;
            }

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        private void EndChargePhysics()
        {
            if (rb == null || !hasSavedPhysicsState) return;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = originalGravityScale;
            rb.bodyType = originalBodyType;
            hasSavedPhysicsState = false;
            hasExpectedPosition = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════
//  패턴 2 : 살점 흩뿌리기
// ═══════════════════════════════════════════════════════════════
public class FleshScatterNode : BTNode
{
    private FleshBossController boss;
    private int   scatterCount;
    private float force;
    private float fireInterval;
    private BossAttackData attackData;

    private enum Phase { Idle, Telegraph, Attack }
    private Phase     phase       = Phase.Idle;
    private int       fired       = 0;
    private float     nextFire    = 0f;
    private const float ScatterRange = 7f;

    public FleshScatterNode(BossBlackboard bb, FleshBossController boss,
        int scatterCount = 12, float force = 10f, float fireInterval = 0.05f,
        BossAttackData data = null) : base(bb)
    {
        this.boss          = boss;
        this.scatterCount  = scatterCount;
        this.force         = force;
        this.fireInterval  = fireInterval;
        this.attackData    = data;
    }

    public override void OnEnter() { phase = Phase.Idle; fired = 0; }

    protected override NodeState OnEvaluate()
    {
        switch (phase)
        {
            case Phase.Idle:
            {
                if (boss.IsExecutingPattern) return NodeState.Failure;

                // 목적지 미리 랜덤 결정
                phase = Phase.Telegraph;
                boss.SetExecutingPattern(true);
                boss.SetTelegraphing(true);

                // 텔레그래프: 도달 범위 원형 표시
                boss.SpawnTelegraph(attackData,
                    TelegraphShape.Circle, radius: ScatterRange,
                    followBoss: true, onComplete: OnTelegraphDone);

                return NodeState.Running;
            }

            case Phase.Telegraph:
                return NodeState.Running;

            case Phase.Attack:
            {
                if (Time.time >= nextFire && fired < scatterCount)
                {
                    float angle = (360f / scatterCount) * fired;
                    Vector3 dir = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
                    dir.z = 0f;
                    boss.SpawnFleshProjectile(boss.transform.position,
                                              dir.normalized, force,
                                              bounces: 0, isLarge: false,
                                              sizeScale: 0.75f,
                                              maxTravelRange: ScatterRange,
                                              lifetime: 10f,
                                              stopAtMaxRange: true);

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
    // ═══════════════════════════════════════════════════════════════
    public class FleshThrowNode : BTNode
    {
        private FleshBossController boss;
        private int   bounceCount;
        private float throwForce;
        private int   throwCount;
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
                    targetPos = blackboard.PlayerTransform.position;
                    phase     = Phase.Telegraph;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);
                    boss.SpawnTelegraphAt(targetPos, attackData,
                        TelegraphShape.Circle, radius: 1.5f,
                        onComplete: OnTelegraphDone);
                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Attack:
                    if (Time.time >= nextThrow && thrown < throwCount)
                    {
                        boss.SpawnFleshProjectileToTarget(boss.transform.position,
                            targetPos, throwForce,
                            bounces: bounceCount, isLarge: true,
                            bounceTelegraphData: attackData,
                            sizeScale: 1.25f);
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
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 4 : 근접 주먹
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
                    boss.SpawnTelegraph(attackData,
                        TelegraphShape.Circle, radius: smashRadius,
                        followBoss: true, onComplete: OnTelegraphDone);
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
    //  패턴 5 : 살점 흡수
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

            if (!isActive)
            {
                if (boss.IsExecutingPattern) return NodeState.Failure;

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

            if (absorbed) { isActive = false; return NodeState.Success; }
            return NodeState.Running;
        }
    }
}
