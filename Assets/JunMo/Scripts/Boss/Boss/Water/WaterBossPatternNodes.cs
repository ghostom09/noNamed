using UnityEngine;
using BossSystem.BehaviorTree;
using BossSystem;
using BossSystem.Scripable;

namespace BossSystem.Boss.WaterBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  패턴 1 : 십자 물줄기
    //  텔레그래프: 원형(보스 중심), 반지름=beamLength
    //  수정: 실패 경로 ForceReleasePattern 보장
    // ═══════════════════════════════════════════════════════════════
    public class CrossWaterBeamNode : BTNode
    {
        private WaterBossController boss;
        private float holdDuration;
        private float rotateSpeed;
        private float targetAngle;
        private float beamDPS;
        private float beamLength;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Attack }
        private Phase phase        = Phase.Idle;
        private float startTime    = 0f;
        private float currentAngle = 0f;

        public CrossWaterBeamNode(BossBlackboard bb, WaterBossController boss,
            float holdDuration = 3f, float rotateSpeed = 15f,
            float targetAngle = 45f, float beamDPS = 25f, float beamLength = 12f,
            BossAttackData data = null) : base(bb)
        {
            this.boss         = boss;
            this.holdDuration = holdDuration;
            this.rotateSpeed  = rotateSpeed;
            this.targetAngle  = targetAngle;
            this.beamDPS      = beamDPS;
            this.beamLength   = beamLength;
            this.attackData   = data;
        }

        public override void OnEnter() { phase = Phase.Idle; currentAngle = 0f; }

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
                        TelegraphShape.Circle, radius: beamLength,
                        followParent: true, onComplete: OnTelegraphDone);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Attack:
                    if (currentAngle < targetAngle)
                    {
                        currentAngle = Mathf.MoveTowards(currentAngle, targetAngle,
                                                         rotateSpeed * Time.deltaTime);
                        boss.SetBeamRotation(currentAngle);
                    }

                    if (Time.time - startTime >= holdDuration)
                    {
                        boss.ActivateCrossBeam(false, 0f);
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
            phase        = Phase.Attack;
            startTime    = Time.time;
            currentAngle = 0f;
            boss.ActivateCrossBeam(true, beamDPS);
            boss.SetTelegraphing(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 2 : 내려치기
    //  텔레그래프: 원형(보스 중심), 반지름=slamRange
    //  수정: 내부 거리 조건 완전 제거 (BT에서 처리 또는 거리 무관)
    // ═══════════════════════════════════════════════════════════════
    public class GroundSlamNode : BTNode
    {
        private WaterBossController boss;
        private float slamRange;
        private float slamDamage;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Windup, Hit }
        private Phase phase      = Phase.Idle;
        private float windupEnd  = 0f;
        private bool  hitApplied = false;

        private const float WINDUP_TIME = 0.3f;
        private const float POST_HIT    = 0.3f;

        public GroundSlamNode(BossBlackboard bb, WaterBossController boss,
            float slamRange = 6f, float slamDamage = 60f,
            BossAttackData data = null) : base(bb)
        {
            this.boss       = boss;
            this.slamRange  = slamRange;
            this.slamDamage = slamDamage;
            this.attackData = data;
        }

        public override void OnEnter() { phase = Phase.Idle; hitApplied = false; }

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
                        TelegraphShape.Circle, radius: slamRange,
                        followParent: true, onComplete: OnTelegraphDone);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Windup:
                    if (Time.time >= windupEnd)
                    {
                        phase      = Phase.Hit;
                        hitApplied = false;
                    }
                    return NodeState.Running;

                case Phase.Hit:
                    if (!hitApplied)
                    {
                        hitApplied = true;
                        boss.ApplySlamDamage(slamRange, slamDamage);
                        boss.PlaySlamVFX();
                    }
                    if (Time.time >= windupEnd + POST_HIT)
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
            phase     = Phase.Windup;
            windupEnd = Time.time + WINDUP_TIME;
            boss.SetTelegraphing(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 3 : 파도 발사
    //  텔레그래프: 직선(발사 방향), 길이=maxRange, 폭=waveWidth
    // ═══════════════════════════════════════════════════════════════
    public class WaveBlastNode : BTNode
    {
        private WaterBossController boss;
        private float waveWidth;
        private float waveSpeed;
        private float waveDamage;
        private float knockbackForce;
        private float maxRange;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Fire }
        private Phase   phase   = Phase.Idle;
        private Vector3 fireDir;

        public WaveBlastNode(BossBlackboard bb, WaterBossController boss,
            float waveWidth = 8f, float waveSpeed = 14f,
            float waveDamage = 35f, float knockbackForce = 18f, float maxRange = 25f,
            BossAttackData data = null) : base(bb)
        {
            this.boss           = boss;
            this.waveWidth      = waveWidth;
            this.waveSpeed      = waveSpeed;
            this.waveDamage     = waveDamage;
            this.knockbackForce = knockbackForce;
            this.maxRange       = maxRange;
            this.attackData     = data;
        }

        public override void OnEnter() => phase = Phase.Idle;

        protected override NodeState OnEvaluate()
        {
            switch (phase)
            {
                case Phase.Idle:
                    if (boss.IsExecutingPattern) return NodeState.Failure;
                    var player = blackboard.PlayerTransform;
                    if (player == null) return NodeState.Failure;

                    fireDir   = (player.position - boss.transform.position).normalized;
                    fireDir.z = 0f;

                    phase = Phase.Telegraph;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);

                    TelegraphHelper.SpawnLine(
                        boss.transform.position,
                        new Vector2(fireDir.x, fireDir.y),
                        length: maxRange, width: waveWidth,
                        attackData, onComplete: OnTelegraphDone);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Fire:
                    boss.SpawnWave(boss.transform.position, fireDir,
                                   waveWidth, waveSpeed, waveDamage, knockbackForce, maxRange);
                    boss.SetExecutingPattern(false);
                    phase = Phase.Idle;
                    return NodeState.Success;
            }
            boss.ForceReleasePattern();
            phase = Phase.Idle;
            return NodeState.Failure;
        }

        private void OnTelegraphDone()
        {
            phase = Phase.Fire;
            boss.SetTelegraphing(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 4 : 물기둥 속박
    //  텔레그래프: 원형(플레이어 위치 고정), 반지름=pillarRadius
    // ═══════════════════════════════════════════════════════════════
    public class WaterPillarBindNode : BTNode
    {
        private WaterBossController boss;
        private float pillarRadius;
        private float pillarDamage;
        private float bindDuration;
        private BossAttackData attackData;

        private enum Phase { Idle, Telegraph, Fire }
        private Phase   phase     = Phase.Idle;
        private bool    fired     = false;
        private Vector3 targetPos;

        public WaterPillarBindNode(BossBlackboard bb, WaterBossController boss,
            float pillarRadius = 2.5f, float pillarDamage = 45f,
            float bindDuration = 2.5f, BossAttackData data = null) : base(bb)
        {
            this.boss         = boss;
            this.pillarRadius = pillarRadius;
            this.pillarDamage = pillarDamage;
            this.bindDuration = bindDuration;
            this.attackData   = data;
        }

        public override void OnEnter() { phase = Phase.Idle; fired = false; }

        protected override NodeState OnEvaluate()
        {
            switch (phase)
            {
                case Phase.Idle:
                    if (boss.IsExecutingPattern) return NodeState.Failure;
                    var player = blackboard.PlayerTransform;
                    if (player == null) return NodeState.Failure;

                    targetPos = player.position;
                    phase     = Phase.Telegraph;
                    boss.SetExecutingPattern(true);
                    boss.SetTelegraphing(true);

                    TelegraphHelper.SpawnAt(targetPos, attackData,
                        TelegraphShape.Circle, radius: pillarRadius,
                        onComplete: OnTelegraphDone);

                    return NodeState.Running;

                case Phase.Telegraph:
                    return NodeState.Running;

                case Phase.Fire:
                    if (!fired)
                    {
                        fired = true;
                        boss.SpawnWaterPillar(targetPos, pillarRadius, pillarDamage, bindDuration);
                        boss.SetTelegraphing(false);
                        boss.SetExecutingPattern(false);
                    }
                    phase = Phase.Idle;
                    return NodeState.Success;
            }
            boss.ForceReleasePattern();
            phase = Phase.Idle;
            return NodeState.Failure;
        }

        private void OnTelegraphDone()
        {
            phase = Phase.Fire;
            boss.SetTelegraphing(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  페이즈 2 : 범람 (IsExecutingPattern 체크 없음 — Parallel 상시)
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
            if (!activated) { activated = true; boss.ActivateFloodField(); }
            return NodeState.Running;
        }
    }
}