using UnityEngine;
using System.Collections.Generic;
using BossSystem.BehaviorTree;
using BossSystem.Boss;
using BossSystem;
using BossSystem.Scripable;

namespace BossSystem.Boss.FleshBoss
{
    public class FleshBossController : BossBase
    {
        [Header("살점 보스 설정")]
        [SerializeField] private float meleeRange = 3.5f;

        [Header("추격")]
        [SerializeField] private float chaseSpeed            = 1.5f;
        [SerializeField] private float chaseStoppingDistance = 2f;

        [Header("쿨다운")]
        [SerializeField] private float chargeCooldown  = 6f;
        [SerializeField] private float scatterCooldown = 5f;
        [SerializeField] private float throwCooldown   = 4f;
        [SerializeField] private float meleeCooldown   = 3f;
        [SerializeField] private float absorbCooldown  = 10f;

        [Header("패턴 텔레그래프 SO")]
        [SerializeField] private BossAttackData chargeData;
        [SerializeField] private BossAttackData scatterData;
        [SerializeField] private BossAttackData throwData;
        [SerializeField] private BossAttackData meleeData;

        [Header("프리팹")]
        [SerializeField] private GameObject fleshChunkPrefab;
        [SerializeField] private GameObject fleshChunkLargePrefab;
        [SerializeField] private GameObject trailZonePrefab;
        [SerializeField] private GameObject absorbVFXPrefab;

        private List<FleshChunk> activeChunks     = new List<FleshChunk>();
        private GameObject       absorbVFXInstance;
        private bool             isCharging        = false;
        private float            chargeDamage      = 40f;

        protected override bool  ShouldChasePlayer     => !isCharging;
        protected override float ChaseSpeed            => chaseSpeed;
        protected override float ChaseStoppingDistance => chaseStoppingDistance;

        public Rigidbody2D GetRigidbody() => rb;

        protected override BTNode BuildBehaviorTree()
        {
            var bb = blackboard;

            var charge = new CooldownNode(bb,
                new ChargeNode(bb, this, chargeSpeed: 16f, chargeDuration: 1.2f,
                               collisionDamage: chargeDamage, data: chargeData),
                "Charge", chargeCooldown);

            var scatter = new CooldownNode(bb,
                new FleshScatterNode(bb, this, scatterCount: 16, speed: 6f,
                    scatterRadius: 8f, data: scatterData),
                "FleshScatter", scatterCooldown);

            var throwNode = new CooldownNode(bb,
                new FleshThrowNode(bb, this, bounceCount: 3, throwForce: 14f,
                                   throwCount: 1, data: throwData),
                "FleshThrow", throwCooldown);

            var melee = new CooldownNode(bb,
                new MeleeSmashNode(bb, this, smashRange: meleeRange, smashDamage: 55f,
                                   data: meleeData),
                "MeleeSmash", meleeCooldown);

            var absorb = new CooldownNode(bb,
                new FleshAbsorbNode(bb, this, absorbRadius: 12f, healPerChunk: 40f),
                "FleshAbsorb", absorbCooldown);

            var meleeSequence = new SequenceNode(bb)
                .AddChild(new ConditionNode(bb, () => bb.DistanceToPlayer <= meleeRange))
                .AddChild(melee);

            var absorbSequence = new SequenceNode(bb)
                .AddChild(new ConditionNode(bb, () => bb.IsPhase2 && HasNearbyChunks(12f)))
                .AddChild(absorb);

            return new SelectorNode(bb)
                .AddChild(absorbSequence)
                .AddChild(meleeSequence)
                .AddChild(charge)
                .AddChild(throwNode)
                .AddChild(scatter);
        }

        protected override void OnEnterPhase2()
        {
            base.OnEnterPhase2();
            Debug.Log("[FleshBoss] 페이즈2 - 흡수 패턴 활성화!");
        }

        public void SetCharging(bool value) => isCharging = value;

        public void SpawnTrailZone(Vector3 position)
        {
            if (trailZonePrefab == null) return;
            var go   = Instantiate(trailZonePrefab, position, Quaternion.identity);
            var zone = go.GetComponent<FleshTrailZone>();
            zone?.Initialize(20f, 3f);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (!isCharging) return;
            // if (col.gameObject.CompareTag("Player"))
            //     col.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(chargeDamage);
        }

        public void SpawnFleshProjectile(Vector3 position, Vector3 direction,
                                         float force, int bounces, bool isLarge = false)
        {
            var prefab = (isLarge && fleshChunkLargePrefab != null)
                         ? fleshChunkLargePrefab : fleshChunkPrefab;
            if (prefab == null) return;

            var go    = Instantiate(prefab, position,
                                    Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            var chunk = go.GetComponent<FleshChunk>();
            if (chunk != null)
            {
                chunk.Initialize(this,
                    hp:      isLarge ? 60f : 30f,
                    dmg:     isLarge ? 30f : 15f,
                    life:    12f,
                    bounces: bounces);
                RegisterChunk(chunk);
            }

            var rbComp = go.GetComponent<Rigidbody2D>();
            if (rbComp != null)
            {
                rbComp.AddForce(direction * force, ForceMode2D.Impulse);
                rbComp.AddTorque(Random.Range(-5f, 5f), ForceMode2D.Impulse);
            }
        }

        public void SpawnFleshProjectileToTarget(Vector3 position, Vector3 targetPosition,
                                                 float speed, int bounces, bool isLarge = false)
        {
            var prefab = (isLarge && fleshChunkLargePrefab != null)
                         ? fleshChunkLargePrefab : fleshChunkPrefab;
            if (prefab == null) return;

            var go = Instantiate(prefab, position,
                                 Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            var chunk = go.GetComponent<FleshChunk>();
            if (chunk != null)
            {
                chunk.Initialize(this,
                    hp:      isLarge ? 60f : 30f,
                    dmg:     isLarge ? 30f : 15f,
                    life:    12f,
                    bounces: bounces);
                chunk.InitializeTargetedFlight(targetPosition, speed);
                RegisterChunk(chunk);
            }

            var rbComp = go.GetComponent<Rigidbody2D>();
            if (rbComp != null)
            {
                rbComp.linearVelocity = Vector2.zero;
                rbComp.angularVelocity = 0f;
            }
        }

        public void PlaySmashWindup() => Debug.Log("[FleshBoss] 주먹 예비 동작");
        public void PlaySmashHit()    => Debug.Log("[FleshBoss] 주먹 히트!");

        public void ApplySmashDamage(float damage, float radius)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position + transform.up * 2f, radius);
            foreach (var hit in hits)
            {
                // if (hit.CompareTag("Player"))
                //     hit.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            }
        }

        public void StartAbsorbEffect()
        {
            if (absorbVFXPrefab != null)
                absorbVFXInstance = Instantiate(absorbVFXPrefab, transform.position,
                                                Quaternion.identity, transform);
        }

        public void StopAbsorbEffect()
        {
            if (absorbVFXInstance != null) Destroy(absorbVFXInstance);
        }

        public void PullChunksToward(float radius, float speed)
        {
            for (int i = activeChunks.Count - 1; i >= 0; i--)
            {
                if (activeChunks[i] == null) { activeChunks.RemoveAt(i); continue; }
                if (Vector2.Distance(activeChunks[i].transform.position, transform.position) > radius)
                    continue;
                var rbComp = activeChunks[i].GetComponent<Rigidbody2D>();
                if (rbComp == null) continue;
                Vector2 pull = ((Vector2)transform.position
                                - (Vector2)activeChunks[i].transform.position).normalized;
                rbComp.linearVelocity = pull * speed;
            }
        }

        public int AbsorbNearbyChunks(float radius)
        {
            int count = 0;
            for (int i = activeChunks.Count - 1; i >= 0; i--)
            {
                if (activeChunks[i] == null) { activeChunks.RemoveAt(i); continue; }
                if (Vector2.Distance(activeChunks[i].transform.position, transform.position) <= radius)
                {
                    activeChunks[i].AbsorbByBoss();
                    count++;
                }
            }
            Debug.Log($"[FleshBoss] 살점 {count}개 흡수!");
            return count;
        }

        public void Heal(float amount)
        {
            currentHP = Mathf.Min(maxHP, currentHP + amount);
            blackboard.CurrentHP = currentHP;
            Debug.Log($"[FleshBoss] 체력 회복 +{amount:F0}");
        }

        public void SpawnFleshChunksAt(Vector3 center, int count)
        {
            if (!blackboard.IsPhase2 || fleshChunkPrefab == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 1.5f;
                var go = Instantiate(fleshChunkPrefab,
                                     center + (Vector3)offset,
                                     Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
                var chunk = go.GetComponent<FleshChunk>();
                if (chunk != null)
                {
                    chunk.Initialize(this, hp: 30f, dmg: 15f, life: 20f, bounces: 0);
                    RegisterChunk(chunk);
                }
                var rbComp = go.GetComponent<Rigidbody2D>();
                if (rbComp != null)
                {
                    Vector2 away = ((Vector2)go.transform.position - (Vector2)center).normalized;
                    rbComp.AddForce(away * 2f, ForceMode2D.Impulse);
                }
            }
        }

        private void RegisterChunk(FleshChunk chunk)
        {
            activeChunks.Add(chunk);
            chunk.OnDestroyed += c => activeChunks.Remove(c);
            chunk.OnAbsorbed  += c => activeChunks.Remove(c);
        }

        public bool HasNearbyChunks(float radius)
        {
            CleanChunkList();
            foreach (var c in activeChunks)
            {
                if (c == null) continue;
                if (Vector2.Distance(c.transform.position, transform.position) <= radius)
                    return true;
            }
            return false;
        }

        private void CleanChunkList() => activeChunks.RemoveAll(c => c == null);
    }
}
