using BossSystem.Boss.FireBoss;
using UnityEngine;
namespace BossSystem.Boss.WaterBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  십자 빔 세그먼트 (4개로 + 형태 구성)
    //  활성화 중 트리거 안 플레이어에게 DPS 적용
    //  WaterBossController의 beamPivot 자식으로 배치:
    //    세그먼트 0°, 90°, 180°, 270° 각도로 배치 → + 형태
    // ═══════════════════════════════════════════════════════════════
    public class WaterBeamSegment : MonoBehaviour
    {
        private float dps;
        private float tickInterval = 0.1f;
        private float lastTick     = 0f;

        public void Initialize(float damagePerSec)
        {
            dps = damagePerSec;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (Time.time - lastTick < tickInterval) return;
            lastTick = Time.time;
            other.GetComponent<PlayerHealth>()?.TakeDamage(dps * tickInterval);
        }
    }
}