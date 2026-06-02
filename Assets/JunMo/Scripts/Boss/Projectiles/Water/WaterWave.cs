using System.Collections.Generic;
using BossSystem.Boss.FireBoss;
using UnityEngine;
namespace BossSystem.Boss.WaterBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  파도 투사체
    //  넓은 직사각형이 direction 방향으로 직진
    //  플레이어 충돌 시 피해 + 넉백, maxRange 초과 시 자멸
    // ═══════════════════════════════════════════════════════════════
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class WaterWave : MonoBehaviour
    {
        private Vector3 direction;
        private float   speed;
        private float   damage;
        private float   knockbackForce;
        private float   maxRange;
        private Vector3 spawnPos;

        private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

        public void Initialize(Vector3 dir, float spd, float dmg,
                               float knockback, float range, float width)
        {
            direction      = dir.normalized;
            speed          = spd;
            damage         = dmg;
            knockbackForce = knockback;
            maxRange       = range;
            spawnPos       = transform.position;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // 파도 폭 설정
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.size      = new Vector2(maxRange, width);
            }

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale   = 0f;
                rb.isKinematic    = false;
                rb.linearVelocity = direction * speed;
            }
        }

        private void Update()
        {
            if (Vector2.Distance(transform.position, spawnPos) >= maxRange)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || hitTargets.Contains(other)) return;
            hitTargets.Add(other);

            other.GetComponent<PlayerHealth>()?.TakeDamage(damage);

            var rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
        }
    }
}