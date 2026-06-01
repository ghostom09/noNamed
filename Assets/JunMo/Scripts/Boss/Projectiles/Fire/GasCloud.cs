using System;
using UnityEngine;
namespace BossSystem.Boss.FireBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  가스 구름 — 페이즈2 전용
    //  · XZ 평면 범위 판정
    // ═══════════════════════════════════════════════════════════════
    public class GasCloud : MonoBehaviour
    {
        [SerializeField] private float radius          = 4f;
        [SerializeField] private float lifetime        = 12f;
        [SerializeField] private float explosionDamage = 80f;
        [SerializeField] private float explosionRadius = 6f;

        public float  Radius => radius;
        public Action OnExpired;

        private FireBossController boss;
        private float              spawnTime;
        private bool               exploded = false;

        public void Initialize(FireBossController bossRef)
        {
            boss      = bossRef;
            spawnTime = Time.time;

            var sc = GetComponent<CircleCollider2D>();
            if (sc != null) { sc.radius = radius; sc.isTrigger = true; }
        }

        private void Update()
        {
            if (!exploded && Time.time - spawnTime >= lifetime)
                Expire();
        }

        public void Explode()
        {
            if (exploded) return;
            exploded = true;

            // ★ XZ 평면 기준 폭발 판정
            var hits = Physics2D.OverlapCircleAll(
                transform.position,
                explosionRadius
            );
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    Vector2 gasXY    = transform.position;
                    Vector2 playerXY = hit.transform.position;
                    if (Vector2.Distance(gasXY, playerXY) <= explosionRadius)
                        hit.GetComponent<PlayerHealth>()?.TakeDamage(explosionDamage);
                }
            }

            Debug.Log("[GasCloud] 폭발!");
            Destroy(gameObject);
            OnExpired?.Invoke();
        }

        private void Expire()
        {
            exploded = true;
            Destroy(gameObject);
            OnExpired?.Invoke();
        }

        // 투사체/링이 트리거 진입 시 폭발
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (exploded) return;
            if (other.CompareTag("Fireball") || other.CompareTag("FireRing"))
                Explode();
        }
    }
}