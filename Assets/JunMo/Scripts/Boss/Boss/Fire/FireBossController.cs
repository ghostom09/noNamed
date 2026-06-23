using UnityEngine;
using System.Collections.Generic;
using System.Collections;
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
        [SerializeField] private float fallbackPlayerLength = 1f;

        [Header("추격")]
        [SerializeField] private float chaseSpeed            = 4f;
        [SerializeField] private float chaseStoppingDistance = 2f;

        [Header("화염 방사")]
        [SerializeField] private float breathDuration = 2.5f;
        [SerializeField] private float breathDPS      = 30f;
        [SerializeField] private float breathCooldown = 4f;
        [SerializeField] private float breathTelegraphDelay = 0.3f;

        [Header("화염탄 난사")]
        [SerializeField] private int   barrageShotCount = 8;
        [SerializeField] private float barrageSpread    = 30f;
        [SerializeField] private float barrageCooldown  = 5f;
        [SerializeField] private float fireballPlayerScale = 0.5f;

        [Header("불의 고리")]
        [SerializeField] private int   ringCount    = 3;
        [SerializeField] private float ringCooldown = 7f;
        [SerializeField] private float ringTelegraphDelay = 1f;
        [SerializeField] private float ringInterval = 0.3f;
        [SerializeField] private float ringPlayerLength = 1.5f;

        [Header("Phase 2 Gas")]
        [SerializeField] private float gasPlayerScale = 2f;
        [SerializeField] private float gasTravelDistance = 5f;
        [SerializeField] private float gasSpeed = 8f;
        [SerializeField] private float gasExplosionTileSize = 1f;

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
            float playerLength = GetPlayerLength();
            float breathRange = playerLength * 2.5f;
            float fireRingWidth = playerLength * ringPlayerLength;
            float fireRingMaxRadius = fireRingWidth * Mathf.Max(1, ringCount);

            closeRange = breathRange;

            var flameBreath = new CooldownNode(bb,
                new FlameBreathNode(bb, this, breathRange, 60f, breathDPS, breathDuration,
                    breathTelegraphDelay,
                    data: flameBreathData),
                "FlameBreath", breathCooldown);

            var fireballBarrage = new CooldownNode(bb,
                new FireballBarrageNode(bb, this, farRange, barrageShotCount, barrageSpread,
                    data: fireballBarrageData),
                "FireballBarrage", barrageCooldown);

            var fireRing = new CooldownNode(bb,
                new ExpandingFireRingNode(bb, this, ringCount, ringInterval,
                    maxRadius: fireRingMaxRadius, ringWidth: fireRingWidth,
                    telegraphDelay: ringTelegraphDelay, data: fireRingData),
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
            go.transform.localScale = Vector3.one * GetFireballSize();
            go.GetComponent<FireballProjectile>()
              ?.Initialize(direction, speed, isGasTrigger, this);
        }

        public void SpawnFireRing(Vector3 center, float expandSpeed, float maxRadius,
                                   float damage, float delay, bool isGasTrigger,
                                   bool useCenterPrefab = false, bool useSecondPrefab = false,
                                   float innerRadius = 0f)
        {
            GameObject prefab = useCenterPrefab
                ? fireRingCenterPrefab != null ? fireRingCenterPrefab : fireRingPrefab
                : fireRingDonutPrefab != null ? fireRingDonutPrefab : fireRingPrefab;

            if (prefab != null && prefab.GetComponent<FireRing>() == null)
                prefab = fireRingPrefab;

            if (prefab == null) return;
            float targetRadius = useCenterPrefab ? GetFireRingCenterRadius() : maxRadius;
            var go = Instantiate(prefab, center, Quaternion.identity);
            go.GetComponent<FireRing>()
              ?.Initialize(expandSpeed, innerRadius, targetRadius, damage, delay, isGasTrigger, this, useCenterPrefab);
        }

        public float GetFireRingCenterRadius()
        {
            return GetPlayerLength() * ringPlayerLength;
        }

        public float GetFireballSize()
        {
            return GetPlayerLength() * fireballPlayerScale;
        }

        public float GetPlayerLength()
        {
            Transform target = GetPlayer();
            if (target == null)
                return Mathf.Max(0.01f, fallbackPlayerLength);

            return Mathf.Max(target.lossyScale.x, target.lossyScale.y, fallbackPlayerLength, 0.01f);
        }

        public void SpawnFireRingTelegraphs(BossAttackData data, float maxRadius,
                                            System.Action onComplete,
                                            float duration = -1f)
        {
            var maker = new TelegraphMaker();
            float centerRadius = GetFireRingCenterRadius();

            maker.SpawnCircle(
                fireRingCenterTelegraphPrefab != null ? fireRingCenterTelegraphPrefab : circleTelegraphPrefab,
                transform.position,
                centerRadius,
                data,
                parent: transform,
                duration: duration);

            maker.SpawnCircle(
                fireRingDonutTelegraphPrefab != null ? fireRingDonutTelegraphPrefab : circleTelegraphPrefab,
                transform.position,
                maxRadius,
                data,
                onComplete,
                transform,
                duration);
        }

        public void SpawnFireRingTelegraph(BossAttackData data, float innerRadius, float radius,
                                           bool useCenterTelegraph,
                                           System.Action onComplete,
                                           float duration = -1f)
        {
            StartCoroutine(SpawnFireRingFillTelegraph(innerRadius, radius, onComplete, duration));
        }

        private IEnumerator SpawnFireRingFillTelegraph(float innerRadius, float outerRadius,
                                                       System.Action onComplete,
                                                       float duration)
        {
            float telegraphDuration = duration > 0f ? duration : 0.5f;
            GameObject go = new GameObject("Fire Ring Telegraph");
            go.transform.SetParent(transform);
            go.transform.position = transform.position;

            LineRenderer line = go.AddComponent<LineRenderer>();
            SetupRingLine(line, innerRadius, innerRadius, new Color(1f, 0f, 0f, 0.45f), 2);

            float elapsed = 0f;
            while (elapsed < telegraphDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / telegraphDuration);
                float currentOuterRadius = Mathf.Lerp(innerRadius, outerRadius, t);
                SetupRingLine(line, innerRadius, currentOuterRadius, new Color(1f, 0f, 0f, 0.45f), 2);
                yield return null;
            }

            SetupRingLine(line, innerRadius, outerRadius, new Color(1f, 0f, 0f, 0.45f), 2);

            Destroy(go);
            onComplete?.Invoke();
        }

        public static void SetupRingLine(LineRenderer line, float innerRadius, float outerRadius,
                                         Color color, int sortingOrder)
        {
            const int segments = 96;
            float width = Mathf.Max(0.01f, outerRadius - innerRadius);
            float radius = innerRadius + width * 0.5f;

            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = segments;
            line.widthMultiplier = width;
            line.sortingOrder = sortingOrder;
            line.startColor = color;
            line.endColor = color;

            if (line.sharedMaterial == null)
                line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

            for (int i = 0; i < segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        public void SpawnGasCloud(Vector3 center, float spreadAngle)
        {
            if (gasCloudPrefab == null || !blackboard.IsPhase2) return;
            Vector3 direction = blackboard.PlayerTransform != null
                ? (blackboard.PlayerTransform.position - center).normalized
                : transform.up;

            var go  = Instantiate(gasCloudPrefab, center, Quaternion.identity);
            var gas = go.GetComponent<GasCloud>();
            if (gas != null)
            {
                gas.Initialize(this, direction, gasSpeed, gasTravelDistance,
                    GetPlayerLength() * gasPlayerScale, gasExplosionTileSize);
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
                if (!gas.IsSpread) continue;
                if (Vector2.Distance(gas.transform.position, p.position) <= gas.Radius)
                {
                    ApplyDamageToPlayer(p.gameObject, damage);
                    break;
                }
            }
        }

        public static bool ApplyDamageToPlayer(GameObject target, float damage)
        {
            if (target == null) return false;

            var health = target.GetComponent<PlayerHealth>();
            if (health == null) return false;

            health.TakeDamage(damage);
            return true;
        }
    }
}
