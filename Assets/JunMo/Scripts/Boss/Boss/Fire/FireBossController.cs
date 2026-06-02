using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BossSystem.BehaviorTree;
using BossSystem.Boss;

namespace BossSystem.Boss.FireBoss
{
    /// <summary>
    /// 화염 보스 컨트롤러
    /// BuildBehaviorTree()에서 패턴 노드들을 조합하여 AI 완성
    ///
    /// ┌─ Parallel ──────────────────────────────────────────┐
    /// │  ├─ [Phase2] GasDotDamage (항상 병행)              │
    /// │  └─ Selector (주 패턴 선택)                        │
    /// │       ├─ Sequence [근접 시]                        │
    /// │       │    ├─ Condition: 거리 < closeRange         │
    /// │       │    └─ Cooldown(FlameBreath)                │
    /// │       ├─ Sequence [원거리 시]                      │
    /// │       │    ├─ Condition: 거리 >= farRange          │
    /// │       │    └─ Cooldown(FireballBarrage)            │
    /// │       └─ Cooldown(ExpandingFireRing) [중간 거리]   │
    /// └─────────────────────────────────────────────────────┘
    /// </summary>
    public class FireBossController : BossBase
    {
        [Header("화염 보스 설정")]
        [SerializeField] private float closeRange = 5f;
        [SerializeField] private float farRange = 8f;

        [Header("추격")]
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField] private float chaseStoppingDistance = 2f;

        [Header("화염 방사")]
        [SerializeField] private float breathDuration = 2.5f;
        [SerializeField] private float breathDPS = 30f;
        [SerializeField] private float breathCooldown = 4f;

        [Header("화염탄 난사")]
        [SerializeField] private int barrageShotCount = 8;
        [SerializeField] private float barrageSpread = 30f;
        [SerializeField] private float barrageCooldown = 5f;

        [Header("불의 고리")]
        [SerializeField] private int ringCount = 3;
        [SerializeField] private float ringCooldown = 7f;

        [Header("프리팹")]
        [SerializeField] private GameObject fireballPrefab;
        [SerializeField] private GameObject fireRingPrefab;
        [SerializeField] private GameObject gasCloudPrefab;
        [SerializeField] private GameObject flameBreathVFX;

        // 활성화된 가스 구름 목록 (페이즈2 폭발 연동)
        private List<GasCloud> activeGasClouds = new List<GasCloud>();
        public IReadOnlyList<GasCloud> ActiveGasClouds => activeGasClouds;

        protected override float ChaseSpeed => chaseSpeed;
        protected override float ChaseStoppingDistance => chaseStoppingDistance;

        // ── 비헤이비어 트리 구성 ─────────────────────────────
        protected override BTNode BuildBehaviorTree()
        {
            var bb = blackboard;

            // 패턴 노드 생성
            var flameBreath = new CooldownNode(bb,
                new FlameBreathNode(bb, this, closeRange, 90f, breathDPS, breathDuration),
                "FlameBreath", breathCooldown);

            var fireballBarrage = new CooldownNode(bb,
                new FireballBarrageNode(bb, this, farRange, barrageShotCount, barrageSpread),
                "FireballBarrage", barrageCooldown);

            var fireRing = new CooldownNode(bb,
                new ExpandingFireRingNode(bb, this, ringCount),
                "FireRing", ringCooldown);

            var gasDot = new GasDotDamageNode(bb, this, 15f);

            // 근접 시퀀스: 거리 조건 + 화염 방사
            var closeSequence = new SequenceNode(bb)
                .AddChild(new ConditionNode(bb, () => bb.DistanceToPlayer <= closeRange))
                .AddChild(flameBreath);

            // 원거리 시퀀스: 거리 조건 + 화염탄 난사
            var farSequence = new SequenceNode(bb)
                .AddChild(new ConditionNode(bb, () => bb.DistanceToPlayer >= farRange))
                .AddChild(fireballBarrage);

            // 주 패턴 선택기 (우선순위: 근접 > 원거리 > 불의 고리)
            var patternSelector = new SelectorNode(bb)
                .AddChild(closeSequence)
                .AddChild(farSequence)
                .AddChild(fireRing);

            // 루트: 가스 도트 + 패턴 병행 실행
            var root = new ParallelNode(bb, 1) // 패턴 하나만 성공해도 OK
                .AddChild(gasDot)              // 페이즈2 가스 도트 (항상)
                .AddChild(patternSelector);

            return root;
        }

        protected override void OnEnterPhase2()
        {
            base.OnEnterPhase2();
            // 페이즈2 진입 이펙트 등 추가 가능
            Debug.Log("[FireBoss] 페이즈2 - 가스 방출 시작!");
        }

        // ── 패턴 노드가 호출하는 스폰/이펙트 메서드 ────────────

        public void PlayFlameBreathVFX(bool on)
        {
            if (flameBreathVFX)
                flameBreathVFX.SetActive(on);
        }

        public void SpawnFireball(Vector3 position, Vector3 direction, float speed, bool isGasTrigger)
        {
            if (fireballPrefab == null) return;
            float angle =
                Mathf.Atan2(direction.y, direction.x)
                * Mathf.Rad2Deg;

            Quaternion rot =
                Quaternion.Euler(0, 0, angle - 90f);
            var go = Instantiate(fireballPrefab, position, rot);
            var fb = go.GetComponent<FireballProjectile>();
            fb?.Initialize(direction, speed, isGasTrigger, this);
        }

        public void SpawnFireRing(Vector3 center, float expandSpeed, float maxRadius,
                                   float damage, float delay, bool isGasTrigger)
        {
            if (fireRingPrefab == null) return;
            var go = Instantiate(fireRingPrefab, center, Quaternion.identity);
            var ring = go.GetComponent<FireRing>();
            ring?.Initialize(expandSpeed, maxRadius, damage, delay, isGasTrigger, this);
        }

        public void SpawnGasCloud(Vector3 center, float spreadAngle)
        {
            if (gasCloudPrefab == null || !blackboard.IsPhase2) return;
            var go = Instantiate(gasCloudPrefab, center, Quaternion.identity);
            var gas = go.GetComponent<GasCloud>();
            if (gas != null)
            {
                gas.Initialize(this);
                activeGasClouds.Add(gas);
                gas.OnExpired += () => activeGasClouds.Remove(gas);
            }
        }

        /// <summary>투사체/링이 가스에 닿으면 해당 가스를 폭발</summary>
        public void TriggerGasExplosion(Vector3 position, float radius)
        {
            for (int i = activeGasClouds.Count - 1; i >= 0; i--)
            {
                if (activeGasClouds[i] == null) { activeGasClouds.RemoveAt(i); continue; }
                float dist = Vector2.Distance(activeGasClouds[i].transform.position, position);
                if (dist <= radius + activeGasClouds[i].Radius)
                    activeGasClouds[i].Explode();
            }
        }

        public void ApplyGasDotToPlayer(float damage)
        {
            var player = GetPlayer();
            if (player == null) return;

            foreach (var gas in activeGasClouds)
            {
                if (gas == null) continue;
                if (Vector2.Distance(gas.transform.position, player.position) <= gas.Radius)
                {
                    // player.GetComponent<PlayerHealth>()?.TakeDamage(damage);
                    break; // 여러 가스 중복 적용 방지
                }
            }
        }
    }
}
