using UnityEngine;
using System.Collections.Generic;
using BossSystem.BehaviorTree;
using BossSystem.Boss;
using BossSystem;
using BossSystem.Scripable;

namespace BossSystem.Boss.FireBoss
{
    public class FireBossController : BossBase
    {
        [Header("화염 보스 설정")]
        [SerializeField] private float closeRange = 5f;
        [SerializeField] private float farRange   = 8f;

        [Header("추격")]
        [SerializeField] private float chaseSpeed            = 4f;
        [SerializeField] private float chaseStoppingDistance = 2f;

        [Header("화염 방사")]
        [SerializeField] private float breathDuration = 2.5f;
        [SerializeField] private float breathDPS      = 30f;
        [SerializeField] private float breathCooldown = 4f;

        [Header("화염탄 난사")]
        [SerializeField] private int   barrageShotCount = 8;
        [SerializeField] private float barrageSpread    = 30f;
        [SerializeField] private float barrageCooldown  = 5f;

        [Header("불의 고리")]
        [SerializeField] private int   ringCount    = 2;
        [SerializeField] private float ringCooldown = 7f;

        [Header("패턴 텔레그래프 SO")]
        [SerializeField] private BossAttackData flameBreathData;
        [SerializeField] private BossAttackData fireballBarrageData;
        [SerializeField] private BossAttackData fireRingData;

        [Header("프리팹")]
        [SerializeField] private GameObject fireballPrefab;
        [SerializeField] private GameObject fireRingPrefab;
        [SerializeField] private GameObject fireRingCenterPrefab;
        [SerializeField] private GameObject fireRingDonutPrefab;
        [SerializeField] private GameObject fireRingCenterTelegraphPrefab;
        [SerializeField] private GameObject fireRingDonutTelegraphPrefab;
        [SerializeField] private GameObject gasCloudPrefab;
        [SerializeField] private GameObject flameBreathVFX;

        private List<GasCloud> activeGasClouds = new List<GasCloud>();
        public IReadOnlyList<GasCloud> ActiveGasClouds => activeGasClouds;

        protected override float ChaseSpeed            => chaseSpeed;
        protected override float ChaseStoppingDistance => chaseStoppingDistance;

        protected override BTNode BuildBehaviorTree()
        {
            var bb = blackboard;

            var flameBreath = new CooldownNode(bb,
                new FlameBreathNode(bb, this, closeRange, 90f, breathDPS, breathDuration,
                    data: flameBreathData),
                "FlameBreath", breathCooldown);

            var fireballBarrage = new CooldownNode(bb,
                new FireballBarrageNode(bb, this, farRange, barrageShotCount, barrageSpread,
                    data: fireballBarrageData),
                "FireballBarrage", barrageCooldown);

            var fireRing = new CooldownNode(bb,
                new ExpandingFireRingNode(bb, this, ringCount, data: fireRingData),
                "FireRing", ringCooldown);

            var gasDot = new GasDotDamageNode(bb, this, 15f);

            var closeSequence = new SequenceNode(bb)
                .AddChild(new ConditionNode(bb, () => bb.DistanceToPlayer <= closeRange))
                .AddChild(flameBreath);

            var farSequence = new SequenceNode(bb)
                .AddChild(new ConditionNode(bb, () => bb.DistanceToPlayer >= farRange))
                .AddChild(fireballBarrage);

            var patternSelector = new SelectorNode(bb)
                .AddChild(closeSequence)
                .AddChild(farSequence)
                .AddChild(fireRing);

            return new ParallelNode(bb, 1)
                .AddChild(gasDot)
                .AddChild(patternSelector);
        }

        protected override void OnEnterPhase2()
        {
            base.OnEnterPhase2();
            Debug.Log("[FireBoss] 페이즈2 - 가스 방출 시작!");
        }

        public void PlayFlameBreathVFX(bool on)
        {
            if (flameBreathVFX) flameBreathVFX.SetActive(on);
        }

        public void SpawnFireball(Vector3 position, Vector3 direction,
                                   float speed, bool isGasTrigger)
        {
            if (fireballPrefab == null) return;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var go = Instantiate(fireballPrefab, position,
                                 Quaternion.Euler(0, 0, angle - 90f));
            go.GetComponent<FireballProjectile>()
              ?.Initialize(direction, speed, isGasTrigger, this);
        }

        public void SpawnFireRing(Vector3 center, float expandSpeed, float maxRadius,
                                   float damage, float delay, bool isGasTrigger,
                                   bool useCenterPrefab = false)
        {
            GameObject prefab = useCenterPrefab
                ? fireRingCenterPrefab != null ? fireRingCenterPrefab : fireRingPrefab
                : fireRingDonutPrefab != null ? fireRingDonutPrefab : fireRingPrefab;

            if (prefab == null) return;
            float targetRadius = useCenterPrefab ? Mathf.Max(1f, closeRange * 0.5f) : maxRadius;
            var go = Instantiate(prefab, center, Quaternion.identity);
            go.GetComponent<FireRing>()
              ?.Initialize(expandSpeed, targetRadius, damage, delay, isGasTrigger, this, useCenterPrefab);
        }

        public void SpawnFireRingTelegraphs(BossAttackData data, float maxRadius,
                                            System.Action onComplete)
        {
            var maker = new TelegraphMaker();
            float centerRadius = Mathf.Max(1f, closeRange * 0.5f);

            maker.SpawnCircle(
                fireRingCenterTelegraphPrefab != null ? fireRingCenterTelegraphPrefab : circleTelegraphPrefab,
                transform.position,
                centerRadius,
                data,
                parent: transform);

            maker.SpawnCircle(
                fireRingDonutTelegraphPrefab != null ? fireRingDonutTelegraphPrefab : circleTelegraphPrefab,
                transform.position,
                maxRadius,
                data,
                onComplete,
                transform);
        }

        public void SpawnGasCloud(Vector3 center, float spreadAngle)
        {
            if (gasCloudPrefab == null || !blackboard.IsPhase2) return;
            var go  = Instantiate(gasCloudPrefab, center, Quaternion.identity);
            var gas = go.GetComponent<GasCloud>();
            if (gas != null)
            {
                gas.Initialize(this);
                activeGasClouds.Add(gas);
                gas.OnExpired += () => activeGasClouds.Remove(gas);
            }
        }

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
            var p = GetPlayer();
            if (p == null) return;
            foreach (var gas in activeGasClouds)
            {
                if (gas == null) continue;
                if (Vector2.Distance(gas.transform.position, p.position) <= gas.Radius)
                {
                    // p.GetComponent<PlayerHealth>()?.TakeDamage(damage);
                    break;
                }
            }
        }
    }
}
