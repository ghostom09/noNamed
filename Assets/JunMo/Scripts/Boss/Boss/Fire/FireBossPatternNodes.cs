using UnityEngine;
using System.Collections;
using BossSystem.BehaviorTree;
using BossSystem.Boss;

namespace BossSystem.Boss.FireBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  패턴 1 : 근접 화염 방사 (FlameBreath)
    //  플레이어가 closeRange 이내 → 전방 부채꼴에 지속 피해
    // ═══════════════════════════════════════════════════════════════
    public class FlameBreathNode : BTNode
    {
        private FireBossController boss;
        private float closeRange;
        private float fanAngle;       // 부채꼴 각도
        private float damagePerSec;
        private float duration;
        private float tickInterval;

        // 실행 상태
        private bool isActive = false;
        private float startTime;
        private float lastTickTime;

        // 블랙보드 키
        private const string KEY_GAS = "GasActive";

        public FlameBreathNode(BossBlackboard bb, FireBossController boss,
            float closeRange = 5f, float fanAngle = 90f,
            float damagePerSec = 30f, float duration = 2.5f)
            : base(bb)
        {
            this.boss = boss;
            this.closeRange = closeRange;
            this.fanAngle = fanAngle;
            this.damagePerSec = damagePerSec;
            this.duration = duration;
            this.tickInterval = 0.2f;
        }

        protected override NodeState OnEvaluate()
        {
            // 아직 시작 전 → 조건 검사
            if (!isActive)
            {
                if (blackboard.DistanceToPlayer > closeRange)
                    return NodeState.Failure;

                isActive = true;
                startTime = Time.time;
                lastTickTime = Time.time;
                boss.PlayFlameBreathVFX(true);
                return NodeState.Running;
            }

            // 실행 중
            float elapsed = Time.time - startTime;

            // 틱 데미지
            if (Time.time - lastTickTime >= tickInterval)
            {
                lastTickTime = Time.time;
                ApplyFanDamage();
            }

            if (elapsed >= duration)
            {
                isActive = false;
                boss.PlayFlameBreathVFX(false);

                // 페이즈2: 가스 생성
                if (blackboard.IsPhase2)
                    boss.SpawnGasCloud(boss.transform.position, fanAngle);

                return NodeState.Success;
            }
            return NodeState.Running;
        }

        private void ApplyFanDamage()
        {
            var player = blackboard.PlayerTransform;
            if (player == null) return;

            Vector3 toPlayer = player.position - boss.transform.position;
            float angle =
                Vector2.Angle(
                    boss.transform.up,
                    toPlayer
                );

            if (angle <= fanAngle * 0.5f && toPlayer.magnitude <= closeRange)
            {
                float dmg = damagePerSec * tickInterval;
                // player.GetComponent<PlayerHealth>()?.TakeDamage(dmg);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 2 : 화염탄 난사 (FireballBarrage)
    //  플레이어가 farRange 이상 → ±30° 무작위 방향으로 투사체 연속 발사
    // ═══════════════════════════════════════════════════════════════
    public class FireballBarrageNode : BTNode
    {
        private FireBossController boss;
        private float farRange;
        private int shotCount;
        private float spreadAngle;    // 기본 30°
        private float fireInterval;
        private float projectileSpeed;

        private bool isActive = false;
        private int shotsFired = 0;
        private float nextFireTime;

        public FireballBarrageNode(BossBlackboard bb, FireBossController boss,
            float farRange = 8f, int shotCount = 8,
            float spreadAngle = 30f, float fireInterval = 0.15f,
            float projectileSpeed = 12f)
            : base(bb)
        {
            this.boss = boss;
            this.farRange = farRange;
            this.shotCount = shotCount;
            this.spreadAngle = spreadAngle;
            this.fireInterval = fireInterval;
            this.projectileSpeed = projectileSpeed;
        }

        protected override NodeState OnEvaluate()
        {
            if (!isActive)
            {
                if (blackboard.DistanceToPlayer < farRange)
                    return NodeState.Failure;

                isActive = true;
                shotsFired = 0;
                nextFireTime = Time.time;
                return NodeState.Running;
            }

            // 연속 발사
            if (Time.time >= nextFireTime && shotsFired < shotCount)
            {
                FireProjectile();
                shotsFired++;
                nextFireTime = Time.time + fireInterval;
            }

            if (shotsFired >= shotCount)
            {
                isActive = false;

                if (blackboard.IsPhase2)
                    boss.SpawnGasCloud(boss.transform.position, 360f);

                return NodeState.Success;
            }
            return NodeState.Running;
        }

        private void FireProjectile()
        {
            var player = blackboard.PlayerTransform;
            if (player == null) return;

            Vector3 baseDir = (player.position - boss.transform.position).normalized;
            float randomAngle = Random.Range(-spreadAngle, spreadAngle);
            Vector3 dir =
                Quaternion.Euler(0, 0, randomAngle)
                * baseDir;

            boss.SpawnFireball(boss.transform.position, dir, projectileSpeed,
                               blackboard.IsPhase2);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  패턴 3 : 확산하는 불의 고리 (ExpandingFireRing)
    //  보스 중심으로 3회 순차 확장 — 타이밍에 맞춰 회피 필요
    // ═══════════════════════════════════════════════════════════════
    public class ExpandingFireRingNode : BTNode
    {
        private FireBossController boss;
        private int ringCount;
        private float ringInterval;    // 각 고리 간격(초)
        private float expandSpeed;
        private float maxRadius;
        private float damage;

        private bool isActive = false;
        private int ringsSpawned = 0;
        private float nextRingTime;

        public ExpandingFireRingNode(BossBlackboard bb, FireBossController boss,
            int ringCount = 3, float ringInterval = 0.8f,
            float expandSpeed = 6f, float maxRadius = 12f, float damage = 40f)
            : base(bb)
        {
            this.boss = boss;
            this.ringCount = ringCount;
            this.ringInterval = ringInterval;
            this.expandSpeed = expandSpeed;
            this.maxRadius = maxRadius;
            this.damage = damage;
        }

        protected override NodeState OnEvaluate()
        {
            if (!isActive)
            {
                isActive = true;
                ringsSpawned = 0;
                nextRingTime = Time.time;
                return NodeState.Running;
            }

            if (Time.time >= nextRingTime && ringsSpawned < ringCount)
            {
                SpawnRing(ringsSpawned);
                ringsSpawned++;
                nextRingTime = Time.time + ringInterval;
            }

            // 마지막 고리가 다 퍼질 때까지 대기
            float totalDuration = ringCount * ringInterval + (maxRadius / expandSpeed);
            if (Time.time >= nextRingTime + (maxRadius / expandSpeed) && ringsSpawned >= ringCount)
            {
                isActive = false;

                if (blackboard.IsPhase2)
                    boss.SpawnGasCloud(boss.transform.position, 360f);

                return NodeState.Success;
            }
            return NodeState.Running;
        }

        private void SpawnRing(int index)
        {
            // 지연 시작으로 시각적 간격 부여
            float delay = index * 0.05f;
            boss.SpawnFireRing(boss.transform.position, expandSpeed, maxRadius, damage, delay,
                               blackboard.IsPhase2);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  페이즈2 가스 안에서 도트 데미지 적용 노드
    // ═══════════════════════════════════════════════════════════════
    public class GasDotDamageNode : BTNode
    {
        private FireBossController boss;
        private float damagePerSec;
        private float tickInterval;
        private float lastTickTime;

        public GasDotDamageNode(BossBlackboard bb, FireBossController boss,
            float damagePerSec = 15f)
            : base(bb)
        {
            this.boss = boss;
            this.damagePerSec = damagePerSec;
            this.tickInterval = 0.5f;
        }

        protected override NodeState OnEvaluate()
        {
            if (!blackboard.IsPhase2) return NodeState.Failure;

            if (Time.time - lastTickTime >= tickInterval)
            {
                lastTickTime = Time.time;
                boss.ApplyGasDotToPlayer(damagePerSec * tickInterval);
            }
            // 항상 Running — 페이즈2 동안 계속 병행 실행
            return NodeState.Running;
        }
    }
}
