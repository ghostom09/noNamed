using UnityEngine;
namespace BossSystem.Boss.FireBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  화염탄 투사체 — 탑다운 2D
    //  · Y축 속도 없음, XZ 평면 이동
    //  · 벽 충돌: "Environment" 태그 대신 비-트리거 Collider2D 전체 반응
    // ═══════════════════════════════════════════════════════════════
    public class FireballProjectile : MonoBehaviour
    {
        private Vector3             direction;
        private float               speed;
        private bool                isGasTrigger;
        private FireBossController  boss;
        private float               damage   = 25f;
        private float               lifetime = 5f;

        public void Initialize(Vector3 dir, float spd, bool gasTrigger, FireBossController bossRef)
        {
            direction = new Vector3(
                dir.x,
                dir.y,
                0f
            ).normalized;
            speed        = spd;
            isGasTrigger = gasTrigger;
            boss         = bossRef;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.isTrigger) return; // 트리거끼리 무시

            if (other.CompareTag("Player"))
            {
                other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
                HandleGasInteraction();
                Destroy(gameObject);
            }
            else
            {
                // ★ 태그 상관없이 비-트리거 콜라이더면 벽으로 처리
                HandleGasInteraction();
                Destroy(gameObject);
            }
        }

        private void HandleGasInteraction()
        {
            if (isGasTrigger && boss != null)
                boss.TriggerGasExplosion(transform.position, 1.5f);
        }
    }
}