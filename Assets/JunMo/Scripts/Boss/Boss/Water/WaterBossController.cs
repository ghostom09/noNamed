using UnityEngine;
using System.Collections.Generic;
using BossSystem.BehaviorTree;
using BossSystem.Boss;

namespace BossSystem.Boss.WaterBoss
{
    /// <summary>
    /// 물 보스 컨트롤러
    ///
    /// ┌─ ROOT: Parallel ──────────────────────────────────────────┐
    /// │  ├─ FloodFieldNode      [Phase2: 상시 둔화 유지]          │
    /// │  └─ SelectorNode        [주 패턴 선택 — 우선순위 순]      │
    /// │       ├─ Sequence [근접 조건 + 내려치기]   ← 패턴 2      │
    /// │       ├─ Cooldown → CrossWaterBeam         ← 패턴 1      │
    /// │       ├─ Cooldown → WaterPillarBind        ← 패턴 4      │
    /// │       └─ Cooldown → WaveBlast              ← 패턴 3      │
    /// └───────────────────────────────────────────────────────────┘
    /// </summary>
    public class WaterBossController : BossBase
    {
        [Header("쿨다운 (초)")]
        [SerializeField] private float beamCooldown   = 8f;
        [SerializeField] private float slamCooldown   = 4f;
        [SerializeField] private float waveCooldown   = 5f;
        [SerializeField] private float pillarCooldown = 6f;

        [Header("패턴 수치")]
        [SerializeField] private float slamRange      = 6f;
        [SerializeField] private float beamDPS        = 25f;
        [SerializeField] private float waveDamage     = 35f;
        [SerializeField] private float waveKnockback  = 18f;
        [SerializeField] private float pillarDamage   = 45f;
        [SerializeField] private float bindDuration   = 2.5f;

        [Header("십자 빔 — Inspector 연결")]
        [Tooltip("4개 빔 세그먼트 (WaterBeamSegment 컴포넌트)")]
        [SerializeField] private WaterBeamSegment[] beamSegments;
        [Tooltip("4개 빔의 공통 부모 — 이 Transform을 Y축 회전시켜 빔 전체 회전")]
        [SerializeField] private Transform          beamPivot;

        [Header("프리팹")]
        [SerializeField] private GameObject waterWavePrefab;
        [SerializeField] private GameObject waterPillarPrefab;
        [SerializeField] private GameObject floodZonePrefab;
        [SerializeField] private GameObject slamVFXPrefab;
        [SerializeField] private GameObject slamWarningDecalPrefab;
        [SerializeField] private GameObject pillarWarningDecalPrefab;

        // 상태
        private bool       floodActive   = false;
        private GameObject floodInstance = null;

        // ── 비헤이비어 트리 구성 ─────────────────────────────────
        protected override BTNode BuildBehaviorTree()
        {
            var bb = blackboard;

            // 패턴 노드 생성
            var crossBeam = new CooldownNode(bb,
                new CrossWaterBeamNode(bb, this,
                    holdDuration: 3f, rotateSpeed: 15f, targetAngle: 45f, beamDPS: beamDPS),
                "CrossBeam", beamCooldown);

            var groundSlam = new CooldownNode(bb,
                new GroundSlamNode(bb, this,
                    slamRange: slamRange, slamDamage: 60f, windupTime: 0.8f),
                "GroundSlam", slamCooldown);

            var waveBlast = new CooldownNode(bb,
                new WaveBlastNode(bb, this,
                    waveWidth: 8f, waveSpeed: 14f,
                    waveDamage: waveDamage, knockbackForce: waveKnockback),
                "WaveBlast", waveCooldown);

            var pillarBind = new CooldownNode(bb,
                new WaterPillarBindNode(bb, this,
                    pillarRadius: 2.5f, pillarDamage: pillarDamage,
                    bindDuration: bindDuration, warningTime: 1.2f),
                "PillarBind", pillarCooldown);

            var flood = new FloodFieldNode(bb, this);

            // 근접 시 내려치기 우선
            var slamSequence = new SequenceNode(bb)
                .AddChild(new ConditionNode(bb, () => bb.DistanceToPlayer <= slamRange * 1.5f))
                .AddChild(groundSlam);

            // 주 패턴 Selector (우선순위: 근접타 > 십자빔 > 기둥 속박 > 파도)
            var patternSelector = new SelectorNode(bb)
                .AddChild(slamSequence)
                .AddChild(crossBeam)
                .AddChild(pillarBind)
                .AddChild(waveBlast);

            // 루트: 범람(Phase2 상시) + 패턴 동시 실행
            return new ParallelNode(bb, 1)
                .AddChild(flood)
                .AddChild(patternSelector);
        }

        protected override void OnEnterPhase2()
        {
            base.OnEnterPhase2();
            Debug.Log("[WaterBoss] 페이즈2 돌입 — 맵 범람 + 전체 둔화!");
        }

        // ══════════════════════════════════════════════════════════
        //  패턴 1 : 십자 빔
        // ══════════════════════════════════════════════════════════

        /// <param name="on">true = 활성화, false = 비활성화</param>
        /// <param name="dps">초당 피해량 (활성화 시에만 의미 있음)</param>
        public void ActivateCrossBeam(bool on, float dps)
        {
            if (beamSegments == null) return;
            foreach (var seg in beamSegments)
            {
                if (seg == null) continue;
                seg.gameObject.SetActive(on);
                if (on) seg.Initialize(dps);
            }
        }

        /// <summary>beamPivot을 Y축 회전시켜 4방향 빔 전체를 회전</summary>
        public void SetBeamRotation(float yAngle)
        {
            if (beamPivot != null)
                beamPivot.rotation = Quaternion.Euler(0f, 0f, yAngle);
        }

        // ══════════════════════════════════════════════════════════
        //  패턴 2 : 내려치기
        // ══════════════════════════════════════════════════════════

        public void ShowSlamWarning(float range)
        {
            if (slamWarningDecalPrefab == null) return;
            var go = Instantiate(slamWarningDecalPrefab, transform.position, Quaternion.identity);
            go.transform.localScale = Vector3.one * (range * 2f);
            Destroy(go, 0.8f);
        }

        public void ApplySlamDamage(float range, float damage)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, range);
            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;
                hit.GetComponent<PlayerHealth>()?.TakeDamage(damage);

                // 방사형 넉백
                var rb = hit.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 away = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                    rb.AddForce(away * 10f, ForceMode2D.Impulse);
                }
            }
        }

        public void PlaySlamVFX()
        {
            if (slamVFXPrefab == null) return;
            var go = Instantiate(slamVFXPrefab, transform.position, Quaternion.identity);
            Destroy(go, 2f);
        }

        // ══════════════════════════════════════════════════════════
        //  패턴 3 : 파도 발사
        // ══════════════════════════════════════════════════════════

        public void SpawnWave(Vector3 origin, Vector3 direction,
                              float width, float speed, float damage,
                              float knockback, float maxRange)
        {
            if (waterWavePrefab == null) return;
            var go   = Instantiate(waterWavePrefab, origin, Quaternion.identity);
            var wave = go.GetComponent<WaterWave>();
            wave?.Initialize(direction, speed, damage, knockback, maxRange, width);
        }

        // ══════════════════════════════════════════════════════════
        //  패턴 4 : 물기둥 속박
        // ══════════════════════════════════════════════════════════

        public void ShowPillarWarning(Vector3 position, float radius)
        {
            if (pillarWarningDecalPrefab == null) return;
            var go = Instantiate(pillarWarningDecalPrefab, position, Quaternion.identity);
            go.transform.localScale = Vector3.one * (radius * 2f);
            Destroy(go, 1.2f);
        }

        public void SpawnWaterPillar(Vector3 position, float radius,
                                     float damage, float bindDur)
        {
            if (waterPillarPrefab == null) return;
            var go     = Instantiate(waterPillarPrefab, position, Quaternion.identity);
            var pillar = go.GetComponent<WaterPillar>();
            pillar?.Initialize(radius, damage, bindDur);
        }

        // ══════════════════════════════════════════════════════════
        //  페이즈 2 : 범람
        // ══════════════════════════════════════════════════════════

        public void ActivateFloodField()
        {
            if (floodActive || floodZonePrefab == null) return;
            floodActive   = true;
            // FloodZone 프리팹은 맵 전체를 덮는 넓은 Trigger Collider를 가짐
            floodInstance = Instantiate(floodZonePrefab, Vector3.zero, Quaternion.identity);
            Debug.Log("[WaterBoss] 맵 전체 범람 — 이동속도 50% 둔화");
        }

        // ══════════════════════════════════════════════════════════
        //  사망 처리
        // ══════════════════════════════════════════════════════════

        protected override void OnDie()
        {
            ActivateCrossBeam(false, 0f);
            if (floodInstance != null) Destroy(floodInstance);
            base.OnDie();
        }
    }
}
