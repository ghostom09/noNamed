using UnityEngine;
using BossSystem.BehaviorTree;

namespace BossSystem.Boss.FleshBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  패턴 1 : 돌진 (ChargeNode)
    //  플레이어 방향으로 고속 돌진 → 데미지 → 지나간 자리에 장판 생성
    // ═══════════════════════════════════════════════════════════════
    public class ChargeNode : BTNode
    {
        private FleshBossController boss;
        private float chargeSpeed;
        private float chargeDuration;
        private float collisionDamage;
        private float trailInterval;    // 장판 생성 간격

        // 실행 상태
        private bool   isCharging    = false;
        private float  chargeEndTime = 0f;
        private Vector3 chargeDir;
        private float  lastTrailTime = 0f;

        public ChargeNode(BossBlackboard bb, FleshBossController boss,
            float chargeSpeed = 16f, float chargeDuration = 1.2f,
            float collisionDamage = 40f, float trailInterval = 0.15f)
            : base(bb)
        {
            this.boss             = boss;
            this.chargeSpeed      = chargeSpeed;
            this.chargeDuration   = chargeDuration;
            this.collisionDamage  = collisionDamage;
            this.trailInterval    = trailInterval;
        }

        public override void OnEnter() => isCharging = false;

        protected override NodeState OnEvaluate()
        {
            if (!isCharging)
            {
                // 돌진 시작: 방향 고정
                var player = blackboard.PlayerTransform;
                if (player == null) return NodeState.Failure;

                chargeDir    = (player.position - boss.transform.position).normalized;
                chargeDir.z  = 0f;
                chargeEndTime = Time.time + chargeDuration;
                lastTrailTime = Time.time;
                isCharging   = true;

                boss.SetCharging(true);
                return NodeState.Running;
            }

            // 이동
            boss.transform.position += chargeDir * chargeSpeed * Time.deltaTime;

            // 장판 생성
            if (Time.time - lastTrailTime >= trailInterval)
            {
                lastTrailTime = Time.time;
                boss.SpawnTrailZone(boss.transform.position);
            }

            // 돌진 종료
            if (Time.time >= chargeEndTime)
            {
                isCharging = false;
                boss.SetCharging(false);

                if (blackboard.IsPhase2)
                    boss.SpawnFleshChunksAt(boss.transform.position, 3);

                return NodeState.Success;
            }
            return NodeState.Running;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 2 : 살점 흩뿌리기 (FleshScatterNode)
    //  360° 모든 방향으로 살점 투사체 다수 발사
    // ═══════════════════════════════════════════════════════════════
    public class FleshScatterNode : BTNode
    {
        private FleshBossController boss;
        private int   scatterCount;
        private float force;
        private float fireInterval;

        private bool  isActive  = false;
        private int   fired     = 0;
        private float nextFire  = 0f;

        public FleshScatterNode(BossBlackboard bb, FleshBossController boss,
            int scatterCount = 16, float force = 10f, float fireInterval = 0.05f)
            : base(bb)
        {
            this.boss          = boss;
            this.scatterCount  = scatterCount;
            this.force         = force;
            this.fireInterval  = fireInterval;
        }

        public override void OnEnter() { isActive = false; fired = 0; }

        protected override NodeState OnEvaluate()
        {
            if (!isActive)
            {
                isActive  = true;
                fired     = 0;
                nextFire  = Time.time;
            }

            if (Time.time >= nextFire && fired < scatterCount)
            {
                // 균등 분산 각도 계산
                float angle = (360f / scatterCount) * fired;
                Vector3 dir = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
                // 위아래 약간 랜덤성 추가
                dir.z = 0f;
                dir.Normalize();

                boss.SpawnFleshProjectile(boss.transform.position,
                                          dir, force, bounces: 0);
                fired++;
                nextFire = Time.time + fireInterval;
            }

            if (fired >= scatterCount)
            {
                isActive = false;

                if (blackboard.IsPhase2)
                    boss.SpawnFleshChunksAt(boss.transform.position, 4);

                return NodeState.Success;
            }
            return NodeState.Running;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 3 : 살점 던지기 (FleshThrowNode)
    //  큰 살점을 플레이어 방향으로 투척 → 3번 튕기고 사라짐
    // ═══════════════════════════════════════════════════════════════
    public class FleshThrowNode : BTNode
    {
        private FleshBossController boss;
        private int   bounceCount;
        private float throwForce;
        private float throwCount;    // 한 번에 던지는 수

        private bool  isActive  = false;
        private int   thrown    = 0;
        private float nextThrow = 0f;

        public FleshThrowNode(BossBlackboard bb, FleshBossController boss,
            int bounceCount = 3, float throwForce = 14f, int throwCount = 1)
            : base(bb)
        {
            this.boss        = boss;
            this.bounceCount = bounceCount;
            this.throwForce  = throwForce;
            this.throwCount  = throwCount;
        }

        public override void OnEnter() { isActive = false; thrown = 0; }

        protected override NodeState OnEvaluate()
        {
            if (!isActive)
            {
                isActive  = true;
                thrown    = 0;
                nextThrow = Time.time;
            }

            if (Time.time >= nextThrow && thrown < throwCount)
            {
                var player = blackboard.PlayerTransform;
                if (player != null)
                {
                    Vector3 toPlayer = (player.position - boss.transform.position).normalized;
                    // 포물선 발사를 위해 y 성분 추가
                    toPlayer.z = 0f;
                    Vector3 launchDir = toPlayer.normalized;

                    boss.SpawnFleshProjectile(boss.transform.position,
                                              launchDir, throwForce,
                                              bounces: bounceCount,
                                              isLarge: true);
                }
                thrown++;
                nextThrow = Time.time + 0.3f;
            }

            if (thrown >= throwCount)
            {
                isActive = false;

                if (blackboard.IsPhase2)
                    boss.SpawnFleshChunksAt(boss.transform.position, 2);

                return NodeState.Success;
            }
            return NodeState.Running;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 4 : 근접 주먹 (MeleeSmashNode)
    //  근거리에서 주먹을 내질러 범위 데미지
    // ═══════════════════════════════════════════════════════════════
    public class MeleeSmashNode : BTNode
    {
        private FleshBossController boss;
        private float smashRange;
        private float smashDamage;
        private float smashRadius;

        // 애니메이션 타이밍 시뮬레이션
        private bool  isActive    = false;
        private float windupEnd   = 0f;
        private float hitEnd      = 0f;
        private bool  hitApplied  = false;

        private const float WINDUP_TIME = 0.4f;   // 예비 동작
        private const float HIT_TIME    = 0.2f;   // 히트 판정

        public MeleeSmashNode(BossBlackboard bb, FleshBossController boss,
            float smashRange = 4f, float smashDamage = 55f, float smashRadius = 3f)
            : base(bb)
        {
            this.boss        = boss;
            this.smashRange  = smashRange;
            this.smashDamage = smashDamage;
            this.smashRadius = smashRadius;
        }

        public override void OnEnter() { isActive = false; hitApplied = false; }

        protected override NodeState OnEvaluate()
        {
            // 조건: 근거리에 있어야 사용
            if (!isActive)
            {
                if (blackboard.DistanceToPlayer > smashRange)
                    return NodeState.Failure;

                isActive   = true;
                hitApplied = false;
                windupEnd  = Time.time + WINDUP_TIME;
                hitEnd     = windupEnd + HIT_TIME;
                boss.PlaySmashWindup();
                return NodeState.Running;
            }

            // 예비 동작 대기
            if (Time.time < windupEnd)
                return NodeState.Running;

            // 히트 판정
            if (!hitApplied)
            {
                hitApplied = true;
                boss.ApplySmashDamage(smashDamage, smashRadius);
                boss.PlaySmashHit();
            }

            if (Time.time >= hitEnd)
            {
                isActive = false;

                if (blackboard.IsPhase2)
                    boss.SpawnFleshChunksAt(boss.transform.position, 3);

                return NodeState.Success;
            }
            return NodeState.Running;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 5 : 살점 흡수 (FleshAbsorbNode)  — Phase2 전용
    //  주변 살점을 끌어당겨 흡수 → 체력 회복
    // ═══════════════════════════════════════════════════════════════
    public class FleshAbsorbNode : BTNode
    {
        private FleshBossController boss;
        private float absorbRadius;
        private float healPerChunk;
        private float pullSpeed;
        private float pullDuration;

        private bool  isActive   = false;
        private float startTime  = 0f;
        private bool  absorbed   = false;

        public FleshAbsorbNode(BossBlackboard bb, FleshBossController boss,
            float absorbRadius = 12f, float healPerChunk = 40f,
            float pullSpeed = 8f, float pullDuration = 1.5f)
            : base(bb)
        {
            this.boss          = boss;
            this.absorbRadius  = absorbRadius;
            this.healPerChunk  = healPerChunk;
            this.pullSpeed     = pullSpeed;
            this.pullDuration  = pullDuration;
        }

        public override void OnEnter() { isActive = false; absorbed = false; }

        protected override NodeState OnEvaluate()
        {
            // 페이즈2 전용 + 주변에 살점이 있어야 발동
            if (!blackboard.IsPhase2) return NodeState.Failure;
            if (!boss.HasNearbyChunks(absorbRadius)) return NodeState.Failure;

            if (!isActive)
            {
                isActive  = true;
                absorbed  = false;
                startTime = Time.time;
                boss.StartAbsorbEffect();
                return NodeState.Running;
            }

            // 살점을 보스 쪽으로 끌어당김
            boss.PullChunksToward(absorbRadius, pullSpeed);

            // 당기는 시간 종료 → 실제 흡수
            if (!absorbed && Time.time >= startTime + pullDuration)
            {
                absorbed = true;
                int count = boss.AbsorbNearbyChunks(absorbRadius * 0.8f);
                boss.Heal(count * healPerChunk);
                boss.StopAbsorbEffect();
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
