using UnityEngine;
using System;
using System.Collections.Generic;

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

    // ═══════════════════════════════════════════════════════════════
    //  불의 고리 — 탑다운 2D
    //  · XY 평면에서 확장
    //  · OverlapSphere → Y를 0으로 고정한 XZ 거리로 판정
    // ═══════════════════════════════════════════════════════════════
    public class FireRing : MonoBehaviour
    {
        private float       expandSpeed;
        private float       maxRadius;
        private float       damage;
        private bool        isGasTrigger;
        private FireBossController boss;

        private float       currentRadius = 0f;
        private float       startTime;
        private bool        started       = false;
        private float       thickness     = 0.8f;

        private HashSet<Collider2D> hitTargets = new ();

        public void Initialize(float spd, float maxR, float dmg, float delay,
                               bool gasTrigger, FireBossController bossRef)
        {
            expandSpeed  = spd;
            maxRadius    = maxR;
            damage       = dmg;
            isGasTrigger = gasTrigger;
            boss         = bossRef;
            startTime    = Time.time + delay;

            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            if (!started)
            {
                if (Time.time < startTime) return;
                started = true;
            }

            currentRadius += expandSpeed * Time.deltaTime;

            // ★ 탑다운: X/Z만 스케일, Y는 얇게 유지
            transform.localScale = new Vector3(currentRadius * 2f, currentRadius * 2f, 1f);

            CheckRingOverlap();

            if (currentRadius >= maxRadius)
            {
                if (isGasTrigger && boss != null)
                    boss.TriggerGasExplosion(transform.position, currentRadius);
                Destroy(gameObject);
            }
        }

        private void CheckRingOverlap()
        {
            // ★ 탑다운: Y를 보스 Y로 고정한 위치에서 OverlapSphere
            Vector2 center = transform.position;
            var hits = Physics2D.OverlapCircleAll(center, currentRadius + thickness * 0.5f);

            foreach (var hit in hits)
            {
                if (hit.isTrigger) continue;
                if (hitTargets.Contains(hit)) continue;

                // XZ 거리로만 링 안쪽 판정
                Vector2 bossXY =
                    new Vector2(center.x, center.y);
                Vector2 hitXY =
                    new Vector2(
                        hit.transform.position.x,
                        hit.transform.position.y
                    );
                float dist = Vector2.Distance(bossXY, hitXY);

                if (dist < currentRadius - thickness * 0.5f) continue; // 내부 제외

                if (hit.CompareTag("Player"))
                {
                    hitTargets.Add(hit);
                    hit.GetComponent<PlayerHealth>()?.TakeDamage(damage);
                }
            }
        }
    }

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

    // ═══════════════════════════════════════════════════════════════
    //  플레이어 체력 (인터페이스 예시)
    // ═══════════════════════════════════════════════════════════════
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHP = 200f;
        private float currentHP;

        private void Awake() => currentHP = maxHP;

        public void TakeDamage(float amount)
        {
            currentHP -= amount;
            Debug.Log($"[Player] HP: {currentHP:F1}/{maxHP} (-{amount:F1})");
            if (currentHP <= 0f) Debug.Log("[Player] 사망");
        }
    }
}
