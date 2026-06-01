using UnityEngine;
using System.Collections.Generic;
using BossSystem.BehaviorTree;
using BossSystem.Boss;

namespace BossSystem.Boss.FleshBoss
{
    /// <summary>
    /// 살점 보스 컨트롤러
    ///
    /// ┌─ ROOT: Selector ──────────────────────────────────────┐
    /// │                                                        │
    /// │  [Phase2 전용] Sequence                               │
    /// │    ├─ Condition: IsPhase2 AND 살점 충분               │
    /// │    └─ Cooldown → FleshAbsorb  (패턴5)                │
    /// │                                                        │
    /// │  Sequence [근접]                                      │
    /// │    ├─ Condition: 거리 ≤ meleeRange                    │
    /// │    └─ Cooldown → MeleeSmash   (패턴4)                │
    /// │                                                        │
    /// │  Cooldown → Charge            (패턴1)                │
    /// │  Cooldown → FleshThrow        (패턴3)                │
    /// │  Cooldown → FleshScatter      (패턴2)                │
    /// └────────────────────────────────────────────────────────┘
    /// </summary>
    public class FleshBossController : BossBase
    {
        [Header("살점 보스 설정")]
        [SerializeField] private float meleeRange = 3.5f;

        [Header("쿨다운")]
        [SerializeField] private float chargeCooldown   = 6f;
        [SerializeField] private float scatterCooldown  = 5f;
        [SerializeField] private float throwCooldown    = 4f;
        [SerializeField] private float meleeCooldown    = 3f;
        [SerializeField] private float absorbCooldown   = 10f;

        [Header("프리팹")]
        [SerializeField] private GameObject fleshChunkPrefab;        // 일반 살점
        [SerializeField] private GameObject fleshChunkLargePrefab;   // 큰 살점 (패턴3)
        [SerializeField] private GameObject trailZonePrefab;         // 장판
        [SerializeField] private GameObject absorbVFXPrefab;         // 흡수 이펙트

        // 활성화된 살점 목록
        private List<FleshChunk> activeChunks = new List<FleshChunk>();

        // 흡수 이펙트 오브젝트
        private GameObject absorbVFXInstance;

        // 돌진 중 충돌 데미지용
        private bool isCharging = false;
        private float chargeDamage = 40f;

        // ── 비헤이비어 트리 구성 ─────────────────────────────────
        protected override BTNode BuildBehaviorTree()
        {
            var bb = blackboard;

            // ── 패턴 노드 ──────────────────────────────────────
            var charge = new CooldownNode(bb,
                new ChargeNode(bb, this, chargeSpeed: 16f, chargeDuration: 1.2f,
                               collisionDamage: chargeDamage),
                "Charge", chargeCooldown);

            var scatter = new CooldownNode(bb,
                new FleshScatterNode(bb, this, scatterCount: 16, force: 10f),
                "FleshScatter", scatterCooldown);

            var throwNode = new CooldownNode(bb,
                new FleshThrowNode(bb, this, bounceCount: 3, throwForce: 14f, throwCount: 1),
                "FleshThrow", throwCooldown);

            var melee = new CooldownNode(bb,
                new MeleeSmashNode(bb, this, smashRange: meleeRange, smashDamage: 55f),
                "MeleeSmash", meleeCooldown);

            var absorb = new CooldownNode(bb,
                new FleshAbsorbNode(bb, this, absorbRadius: 12f, healPerChunk: 40f),
                "FleshAbsorb", absorbCooldown);

            // ── 근접 시퀀스 ────────────────────────────────────
            var meleeSequence = new SequenceNode(bb)
                .AddChild(new ConditionNode(bb, () => bb.DistanceToPlayer <= meleeRange))
                .AddChild(melee);

            // ── 페이즈2 흡수 시퀀스 ────────────────────────────
            var absorbSequence = new SequenceNode(bb)
                .AddChild(new ConditionNode(bb, () => bb.IsPhase2 && HasNearbyChunks(12f)))
                .AddChild(absorb);

            // ── 루트 Selector (우선순위 순서) ──────────────────
            // 1순위: 페이즈2 흡수 (살점이 충분할 때)
            // 2순위: 근접 주먹
            // 3순위: 돌진
            // 4순위: 살점 던지기
            // 5순위: 살점 흩뿌리기
            var root = new SelectorNode(bb)
                .AddChild(absorbSequence)
                .AddChild(meleeSequence)
                .AddChild(charge)
                .AddChild(throwNode)
                .AddChild(scatter);

            return root;
        }

        protected override void OnEnterPhase2()
        {
            base.OnEnterPhase2();
            Debug.Log("[FleshBoss] 페이즈2 - 스킬마다 살점 잔여 + 흡수 패턴 활성화!");
        }

        // ══════════════════════════════════════════════════════
        //  패턴 노드에서 호출하는 메서드들
        // ══════════════════════════════════════════════════════

        // ── 패턴 1: 돌진 ──────────────────────────────────────

        public void SetCharging(bool value) => isCharging = value;

        public void SpawnTrailZone(Vector3 position)
        {
            if (trailZonePrefab == null) return;
            var go   = Instantiate(trailZonePrefab, position, Quaternion.identity);
            var zone = go.GetComponent<FleshTrailZone>();
            // zone?.Initialize(damage: 20f, duration: 3f);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (!isCharging) return;
            if (col.gameObject.CompareTag("Player"))
            {
                
            }
                // col.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(chargeDamage);
        }

        // ── 패턴 2/3: 살점 발사체 ─────────────────────────────

        public void SpawnFleshProjectile(Vector3 position, Vector3 direction,
                                         float force, int bounces, bool isLarge = false)
        {
            var prefab = (isLarge && fleshChunkLargePrefab != null)
                         ? fleshChunkLargePrefab : fleshChunkPrefab;
            if (prefab == null) return;

            var go    = Instantiate(prefab, position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            var chunk = go.GetComponent<FleshChunk>();
            if (chunk != null)
            {
                float hp   = isLarge ? 60f : 30f;
                float dmg  = isLarge ? 30f : 15f;
                chunk.Initialize(this, hp: hp, dmg: dmg, life: 12f, bounces: bounces);
                RegisterChunk(chunk);
            }

            // 물리 발사
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.AddForce(direction * force, ForceMode2D.Impulse);
                rb.AddTorque(Random.Range(-5f, 5f), ForceMode2D.Impulse);
            }
        }

        // ── 패턴 4: 주먹 ──────────────────────────────────────

        public void PlaySmashWindup()
        {
            Debug.Log("[FleshBoss] 주먹 예비 동작");
            // 애니메이터 트리거: animator.SetTrigger("SmashWindup")
        }

        public void ApplySmashDamage(float damage, float radius)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position + transform.up * 2f, radius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    
                }
                    // hit.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            }
        }

        public void PlaySmashHit()
        {
            Debug.Log("[FleshBoss] 주먹 히트!");
            // 카메라 셰이크 등
        }

        // ── 패턴 5: 흡수 ──────────────────────────────────────

        public void StartAbsorbEffect()
        {
            if (absorbVFXPrefab != null)
                absorbVFXInstance = Instantiate(absorbVFXPrefab, transform.position, Quaternion.identity, transform);
            Debug.Log("[FleshBoss] 살점 흡수 시작");
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
                float dist = Vector2.Distance(activeChunks[i].transform.position, transform.position);
                if (dist > radius) continue;

                var rb = activeChunks[i].GetComponent<Rigidbody2D>();
                if (rb == null) continue;

                Vector2 pullDir = ((Vector2)transform.position - (Vector2)activeChunks[i].transform.position).normalized;
                rb.linearVelocity = pullDir * speed;
            }
        }

        public int AbsorbNearbyChunks(float radius)
        {
            int count = 0;
            for (int i = activeChunks.Count - 1; i >= 0; i--)
            {
                if (activeChunks[i] == null) { activeChunks.RemoveAt(i); continue; }
                float dist = Vector2.Distance(activeChunks[i].transform.position, transform.position);
                if (dist <= radius)
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
            Debug.Log($"[FleshBoss] 체력 회복 +{amount:F0} → {currentHP:F0}");
        }

        // ── 페이즈2: 스킬 후 살점 잔여 스폰 ─────────────────

        public void SpawnFleshChunksAt(Vector3 center, int count)
        {
            if (!blackboard.IsPhase2 || fleshChunkPrefab == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 1.5f;
                var go         = Instantiate(fleshChunkPrefab, center + (Vector3)offset, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
                var chunk      = go.GetComponent<FleshChunk>();
                if (chunk != null)
                {
                    chunk.Initialize(this, hp: 30f, dmg: 15f, life: 20f, bounces: 0);
                    RegisterChunk(chunk);
                }
                // 약한 폭발력 추가
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 away = ((Vector2)go.transform.position - (Vector2)center).normalized;
                    rb.AddForce(away * 2f, ForceMode2D.Impulse);
                }
            }
        }

        // ── 살점 관리 ─────────────────────────────────────────

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

        public int NearbyChunkCount(float radius)
        {
            CleanChunkList();
            int count = 0;
            foreach (var c in activeChunks)
            {
                if (c != null && Vector2.Distance(c.transform.position, transform.position) <= radius)
                    count++;
            }
            return count;
        }

        private void CleanChunkList()
        {
            activeChunks.RemoveAll(c => c == null);
        }

        // ── PlayerHealth 참조 (공용 네임스페이스 필요 시 수정) ─
        // private PlayerHealth _playerHealth;
        // private PlayerHealth GetPlayerHealth()
        // {
        //     if (_playerHealth == null && GetPlayer() != null)
        //         _playerHealth = GetPlayer().GetComponent<PlayerHealth>();
        //     return _playerHealth;
        // }
    }
}
