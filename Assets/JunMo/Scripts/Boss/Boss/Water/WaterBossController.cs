using UnityEngine;
using BossSystem.BehaviorTree;
using BossSystem.Boss;
using BossSystem.Boss.FireBoss;
using BossSystem;
using BossSystem.Scripable;

namespace BossSystem.Boss.WaterBoss
{
    public class WaterBossController : BossBase
    {
        [Header("쿨다운 (초)")]
        [SerializeField] private float beamCooldown   = 8f;
        [SerializeField] private float slamCooldown   = 4f;
        [SerializeField] private float waveCooldown   = 5f;
        [SerializeField] private float pillarCooldown = 6f;

        [Header("패턴 수치")]
        [SerializeField] private float slamRange     = 6f;
        [SerializeField] private float beamLength    = 12f;
        [SerializeField] private float beamDPS       = 25f;
        [SerializeField] private float waveDamage    = 35f;
        [SerializeField] private float waveKnockback = 18f;
        [SerializeField] private float pillarDamage  = 45f;
        [SerializeField] private float bindDuration  = 2.5f;

        [Header("패턴 텔레그래프 SO")]
        [SerializeField] private BossAttackData crossBeamData;
        [SerializeField] private BossAttackData groundSlamData;
        [SerializeField] private BossAttackData waveBlastData;
        [SerializeField] private BossAttackData pillarBindData;

        [Header("십자 빔 — Inspector 연결")]
        [SerializeField] private WaterBeamSegment[] beamSegments;
        [SerializeField] private Transform          beamPivot;

        [Header("프리팹")]
        [SerializeField] private GameObject waterWavePrefab;
        [SerializeField] private GameObject waterWaveTelegraph;
        
        [SerializeField] private GameObject waterPillarPrefab;
        [SerializeField] private GameObject waterPillarTelegraph;
        [SerializeField] private GameObject floodZonePrefab;
        [SerializeField] private GameObject floodZoneTelegraph;
        [SerializeField] private GameObject slamVFXPrefab;
        [SerializeField] private GameObject slamVFXTelegraph;

        private bool       floodActive   = false;
        private GameObject floodInstance = null;

        protected override bool ShouldChasePlayer => false;

        protected override BTNode BuildBehaviorTree()
        {
            var bb = blackboard;

            var crossBeam = new CooldownNode(bb,
                new CrossWaterBeamNode(bb, this,
                    holdDuration: 3f, rotateSpeed: 15f, targetAngle: 45f,
                    beamDPS: beamDPS, beamLength: beamLength, data: crossBeamData),
                "CrossBeam", beamCooldown);

            var groundSlam = new CooldownNode(bb,
                new GroundSlamNode(bb, this,
                    slamRange: slamRange, slamDamage: 60f, data: groundSlamData),
                "GroundSlam", slamCooldown);

            var waveBlast = new CooldownNode(bb,
                new WaveBlastNode(bb, this,
                    waveWidth: 8f, waveSpeed: 14f,
                    waveDamage: waveDamage, knockbackForce: waveKnockback,
                    data: waveBlastData),
                "WaveBlast", waveCooldown);

            var pillarBind = new CooldownNode(bb,
                new WaterPillarBindNode(bb, this,
                    pillarRadius: 2.5f, pillarDamage: pillarDamage,
                    bindDuration: bindDuration, data: pillarBindData),
                "PillarBind", pillarCooldown);

            var flood = new FloodFieldNode(bb, this);

            var patternSelector = new SelectorNode(bb)
                .AddChild(groundSlam)
                .AddChild(crossBeam)
                .AddChild(pillarBind)
                .AddChild(waveBlast);

            return new ParallelNode(bb, 1)
                .AddChild(flood)
                .AddChild(patternSelector);
        }

        protected override void OnEnterPhase2()
        {
            base.OnEnterPhase2();
            Debug.Log("[WaterBoss] 페이즈2 — 맵 범람!");
        }

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

        public void SetBeamRotation(float angle)
        {
            if (beamPivot != null)
                beamPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void ApplySlamDamage(float range, float damage)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, range);
            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;
                hit.GetComponent<PlayerHealth>()?.TakeDamage(damage);
                var hitRb = hit.GetComponent<Rigidbody2D>();
                if (hitRb != null)
                {
                    Vector2 away = ((Vector2)hit.transform.position
                                   - (Vector2)transform.position).normalized;
                    hitRb.AddForce(away * 10f, ForceMode2D.Impulse);
                }
            }
        }

        public void PlaySlamVFX()
        {
            if (slamVFXPrefab == null) return;
            var go = Instantiate(slamVFXPrefab, transform.position, Quaternion.identity);
            Destroy(go, 2f);
        }

        public void SpawnWave(Vector3 origin, Vector3 direction,
                              float width, float speed, float damage,
                              float knockback, float maxRange)
        {
            if (waterWavePrefab == null) return;
            var go = Instantiate(waterWavePrefab, origin, Quaternion.identity);
            go.GetComponent<WaterWave>()
              ?.Initialize(direction, speed, damage, knockback, maxRange, width);
        }

        public void SpawnWaterPillar(Vector3 position, float radius,
                                     float damage, float bindDur)
        {
            if (waterPillarPrefab == null) return;
            var go = Instantiate(waterPillarPrefab, position, Quaternion.identity);
            go.GetComponent<WaterPillar>()?.Initialize(radius, damage, bindDur);
        }

        public void ActivateFloodField()
        {
            if (floodActive || floodZonePrefab == null) return;
            floodActive   = true;
            floodInstance = Instantiate(floodZonePrefab, Vector3.zero, Quaternion.identity);
            Debug.Log("[WaterBoss] 맵 전체 범람");
        }

        protected override void OnDie()
        {
            ActivateCrossBeam(false, 0f);
            if (floodInstance != null) Destroy(floodInstance);
            base.OnDie();
        }
    }
}