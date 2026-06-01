using UnityEngine;
using BossSystem.BehaviorTree;

namespace BossSystem.Boss.WaterBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  패턴 1 : 십자 물줄기 (CrossWaterBeam)
    //  4방향 레이저 발사 → holdDuration 유지하며 45° 회전
    //
    //  동작 흐름:
    //   ① 빔 4개 활성화 (0° 기준 +자 배치)
    //   ② rotateSpeed로 매 프레임 회전 → 목표 45° 도달까지
    //   ③ holdDuration 경과 → 빔 비활성화 → Success
    // ═══════════════════════════════════════════════════════════════
    public class CrossWaterBeamNode : BTNode
    {
        private WaterBossController boss;
        private float holdDuration;
        private float rotateSpeed;      // deg/sec
        private float targetAngle;      // 기본 45°
        private float beamDPS;

        private bool  isActive     = false;
        private float startTime    = 0f;
        private float currentAngle = 0f;

        public CrossWaterBeamNode(BossBlackboard bb, WaterBossController boss,
            float holdDuration = 3f, float rotateSpeed = 15f,
            float targetAngle = 45f, float beamDPS = 25f)
            : base(bb)
        {
            this.boss         = boss;
            this.holdDuration = holdDuration;
            this.rotateSpeed  = rotateSpeed;
            this.targetAngle  = targetAngle;
            this.beamDPS      = beamDPS;
        }

        public override void OnEnter()
        {
            isActive     = false;
            currentAngle = 0f;
        }

        protected override NodeState OnEvaluate()
        {
            if (!isActive)
            {
                isActive     = true;
                startTime    = Time.time;
                currentAngle = 0f;
                boss.ActivateCrossBeam(true, beamDPS);
                return NodeState.Running;
            }

            // 45° 목표까지 회전
            if (currentAngle < targetAngle)
            {
                currentAngle = Mathf.MoveTowards(currentAngle, targetAngle,
                                                 rotateSpeed * Time.deltaTime);
                boss.SetBeamRotation(currentAngle);
            }

            if (Time.time - startTime >= holdDuration)
            {
                isActive = false;
                boss.ActivateCrossBeam(false, 0f);
                return NodeState.Success;
            }
            return NodeState.Running;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 2 : 내려치기 (GroundSlam)
    //  보스 주변 원형 범위 강타 — 경고(windupTime) → 충격파 피해
    // ═══════════════════════════════════════════════════════════════
    public class GroundSlamNode : BTNode
    {
        private WaterBossController boss;
        private float slamRange;
        private float slamDamage;
        private float windupTime;

        private bool  isActive   = false;
        private bool  hitApplied = false;
        private float windupEnd  = 0f;

        public GroundSlamNode(BossBlackboard bb, WaterBossController boss,
            float slamRange = 6f, float slamDamage = 60f, float windupTime = 0.8f)
            : base(bb)
        {
            this.boss       = boss;
            this.slamRange  = slamRange;
            this.slamDamage = slamDamage;
            this.windupTime = windupTime;
        }

        public override void OnEnter()
        {
            isActive   = false;
            hitApplied = false;
        }

        protected override NodeState OnEvaluate()
        {
            if (!isActive)
            {
                // 근거리 조건
                if (blackboard.DistanceToPlayer > slamRange * 1.5f)
                    return NodeState.Failure;

                isActive   = true;
                hitApplied = false;
                windupEnd  = Time.time + windupTime;
                boss.ShowSlamWarning(slamRange);
                return NodeState.Running;
            }

            if (Time.time < windupEnd)
                return NodeState.Running;

            if (!hitApplied)
            {
                hitApplied = true;
                boss.ApplySlamDamage(slamRange, slamDamage);
                boss.PlaySlamVFX();
            }

            // 짧은 후딜
            if (Time.time >= windupEnd + 0.3f)
            {
                isActive = false;
                return NodeState.Success;
            }
            return NodeState.Running;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 3 : 파도 발사 (WaveBlast)
    //  플레이어 방향으로 넓은 직사각형 파도 투사체 → 피해 + 넉백
    // ═══════════════════════════════════════════════════════════════
    public class WaveBlastNode : BTNode
    {
        private WaterBossController boss;
        private float waveWidth;
        private float waveSpeed;
        private float waveDamage;
        private float knockbackForce;
        private float maxRange;

        private bool isActive = false;

        public WaveBlastNode(BossBlackboard bb, WaterBossController boss,
            float waveWidth = 8f, float waveSpeed = 14f,
            float waveDamage = 35f, float knockbackForce = 18f, float maxRange = 25f)
            : base(bb)
        {
            this.boss           = boss;
            this.waveWidth      = waveWidth;
            this.waveSpeed      = waveSpeed;
            this.waveDamage     = waveDamage;
            this.knockbackForce = knockbackForce;
            this.maxRange       = maxRange;
        }

        public override void OnEnter() => isActive = false;

        protected override NodeState OnEvaluate()
        {
            if (!isActive)
            {
                var player = blackboard.PlayerTransform;
                if (player == null) return NodeState.Failure;

                isActive = true;
                Vector3 dir = (player.position - boss.transform.position).normalized;
                dir.z = 0f;

                boss.SpawnWave(boss.transform.position, dir,
                               waveWidth, waveSpeed, waveDamage, knockbackForce, maxRange);
                return NodeState.Running;
            }

            // 파도 오브젝트가 스스로 이동 처리 → 즉시 Success
            isActive = false;
            return NodeState.Success;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 4 : 원기둥 속박 (WaterPillarBind)
    //  플레이어 위치에 물기둥 낙하 → 피해 + 속박
    //
    //  동작 흐름:
    //   ① 플레이어 현재 위치 기록 + 경고 마커 표시 (warningTime)
    //   ② warningTime 경과 → 물기둥 소환 → 피해 + 속박 적용
    //   ③ 물기둥이 bindDuration 후 소멸
    // ═══════════════════════════════════════════════════════════════
    public class WaterPillarBindNode : BTNode
    {
        private WaterBossController boss;
        private float pillarRadius;
        private float pillarDamage;
        private float bindDuration;
        private float warningTime;

        private bool    isActive    = false;
        private bool    fired       = false;
        private float   warnEnd     = 0f;
        private Vector3 targetPos;

        public WaterPillarBindNode(BossBlackboard bb, WaterBossController boss,
            float pillarRadius = 2.5f, float pillarDamage = 45f,
            float bindDuration = 2.5f, float warningTime = 1.2f)
            : base(bb)
        {
            this.boss         = boss;
            this.pillarRadius = pillarRadius;
            this.pillarDamage = pillarDamage;
            this.bindDuration = bindDuration;
            this.warningTime  = warningTime;
        }

        public override void OnEnter()
        {
            isActive = false;
            fired    = false;
        }

        protected override NodeState OnEvaluate()
        {
            if (!isActive)
            {
                var player = blackboard.PlayerTransform;
                if (player == null) return NodeState.Failure;

                targetPos = player.position;   // 위치 고정 (텔레그래프)
                isActive  = true;
                fired     = false;
                warnEnd   = Time.time + warningTime;
                boss.ShowPillarWarning(targetPos, pillarRadius);
                return NodeState.Running;
            }

            if (Time.time < warnEnd)
                return NodeState.Running;

            if (!fired)
            {
                fired = true;
                boss.SpawnWaterPillar(targetPos, pillarRadius, pillarDamage, bindDuration);
            }

            isActive = false;
            return NodeState.Success;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  페이즈 2 : 범람 (FloodField)
    //  HP 50% 미만 → 맵 전체 물 장판 생성 → 이동속도 전체 둔화
    //  ParallelNode에 물려 패턴과 동시에 상시 실행
    // ═══════════════════════════════════════════════════════════════
    public class FloodFieldNode : BTNode
    {
        private WaterBossController boss;
        private bool activated = false;

        public FloodFieldNode(BossBlackboard bb, WaterBossController boss)
            : base(bb) => this.boss = boss;

        protected override NodeState OnEvaluate()
        {
            if (!blackboard.IsPhase2) return NodeState.Failure;

            if (!activated)
            {
                activated = true;
                boss.ActivateFloodField();
            }

            // FloodZone 트리거가 직접 처리하므로 Running만 반환
            return NodeState.Running;
        }
    }
}
