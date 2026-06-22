using UnityEngine;
using BossSystem.BehaviorTree;
using BossSystem.Boss;
using BossSystem.Boss.FireBoss;
using BossSystem.Scripable;

namespace BossSystem.Boss.WaterBoss
{
    public class WaterBossController : BossBase
    {
        [Header("Cooldowns")]
        [SerializeField] private float beamCooldown   = 8f;
        [SerializeField] private float slamCooldown   = 4f;
        [SerializeField] private float waveCooldown   = 5f;
        [SerializeField] private float pillarCooldown = 6f;

        [Header("Pattern Values")]
        [SerializeField] private float tileSize      = 1f;
        [SerializeField] private float slamRange     = 3f;
        [SerializeField] private float beamLength    = 60f;
        [SerializeField] private float beamDPS       = 25f;
        [SerializeField] private float waveDamage    = 35f;
        [SerializeField] private float waveKnockback = 18f;
        [SerializeField] private float waveWidth     = 4f;
        [SerializeField] private float waveSpeed     = 5f;
        [SerializeField] private float waveMaxRange  = 25f;
        [SerializeField] private float pillarRadius  = 1.5f;
        [SerializeField] private float pillarDamage  = 45f;
        [SerializeField] private float bindDuration  = 2.5f;
        [SerializeField] private Vector2 floodSize   = new Vector2(100f, 100f);

        [Header("Telegraph Data")]
        [SerializeField] private BossAttackData crossBeamData;
        [SerializeField] private BossAttackData groundSlamData;
        [SerializeField] private BossAttackData waveBlastData;
        [SerializeField] private BossAttackData pillarBindData;

        [Header("Cross Beam")]
        [SerializeField] private WaterBeamSegment[] beamSegments;
        [SerializeField] private Transform          beamPivot;

        [Header("Prefabs")]
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
                    beamDPS: beamDPS, beamLength: beamLength * tileSize, data: crossBeamData),
                "CrossBeam", beamCooldown);

            var groundSlam = new CooldownNode(bb,
                new GroundSlamNode(bb, this,
                    slamRange: slamRange * tileSize, slamDamage: 60f, data: groundSlamData),
                "GroundSlam", slamCooldown);

            var waveBlast = new CooldownNode(bb,
                new WaveBlastNode(bb, this,
                    waveWidth: waveWidth * tileSize, waveSpeed: waveSpeed,
                    waveDamage: waveDamage, knockbackForce: waveKnockback,
                    maxRange: waveMaxRange * tileSize, data: waveBlastData),
                "WaveBlast", waveCooldown);

            var pillarBind = new CooldownNode(bb,
                new WaterPillarBindNode(bb, this,
                    pillarRadius: pillarRadius * tileSize, pillarDamage: pillarDamage,
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
            Debug.Log("[WaterBoss] Phase 2 - flood field activated.");
        }

        public void ActivateCrossBeam(bool on, float dps)
        {
            if (beamSegments == null) return;
            foreach (var seg in beamSegments)
            {
                if (seg == null) continue;
                seg.gameObject.SetActive(on);
                if (on) seg.Initialize(dps, beamLength * tileSize);
            }
        }

        public void SetBeamRotation(float angle)
        {
            if (beamPivot != null)
                beamPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SpawnCrossBeamTelegraphs(float length, BossAttackData data, System.Action onComplete)
        {
            Vector2[] directions = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
            for (int i = 0; i < directions.Length; i++)
            {
                SpawnLineTelegraph(
                    transform.position,
                    directions[i],
                    length,
                    tileSize,
                    data,
                    i == 0 ? onComplete : null);
            }
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
            floodInstance.GetComponent<FloodZone>()?.Initialize(floodSize);
            Debug.Log("[WaterBoss] Flood field spawned.");
        }

        protected override void OnDie()
        {
            ActivateCrossBeam(false, 0f);
            if (floodInstance != null) Destroy(floodInstance);
            base.OnDie();
        }
    }
}
