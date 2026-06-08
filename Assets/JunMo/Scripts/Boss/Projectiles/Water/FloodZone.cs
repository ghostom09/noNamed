using UnityEngine;
namespace BossSystem.Boss.WaterBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  범람 구역 — 페이즈2 맵 전체를 덮는 물 장판
    //  트리거 안 플레이어에게 이동속도 둔화 지속 적용
    // ═══════════════════════════════════════════════════════════════
    public class FloodZone : MonoBehaviour
    {
        [SerializeField] private float slowMultiplier = 0.5f;   // 이동속도 50% 감소
        [SerializeField] private float tickInterval   = 0.25f;
        private float lastTick = 0f;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (Time.time - lastTick < tickInterval) return;
            lastTick = Time.time;
            other.GetComponent<PlayerMovement>()?.ApplySlow(slowMultiplier, tickInterval + 0.1f);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                other.GetComponent<PlayerMovement>()?.RemoveSlow();
        }
    }
}