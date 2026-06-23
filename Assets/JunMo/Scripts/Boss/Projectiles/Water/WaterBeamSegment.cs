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
    [RequireComponent(typeof(BoxCollider2D))]
    public class WaterBeamSegment : MonoBehaviour
    {
        private float dps;
        private float tickInterval = 0.1f;
        private float lastTick     = 0f;

        public void Initialize(float damagePerSec, float length)
        {
            dps = damagePerSec;
            SetLength(length);
        }

        public void SetLength(float length)
        {
            transform.localScale = new Vector3(1f, length, 1f);
            var col = GetComponent<BoxCollider2D>();
            if (col == null)
                col = gameObject.AddComponent<BoxCollider2D>();

            col.isTrigger = true;
            col.size = new Vector2(1f, 1f);
            col.offset = new Vector2(0f, 0.5f);

            var sprite = GetComponent<SpriteRenderer>();
            if (sprite == null)
                return;

            sprite.drawMode = SpriteDrawMode.Simple;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (Time.time - lastTick < tickInterval) return;
            lastTick = Time.time;
            WaterBossController.ApplyDamageToPlayer(other.gameObject, dps * tickInterval);
        }
    }
}
