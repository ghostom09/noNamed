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

        public float  Radius => radius;
        public bool   IsSpread => stopped;
        public Action OnExpired;

        private float              spawnTime;
        private bool               exploded = false;
        private Vector3            moveDirection;
        private Vector3            startPosition;
        private float              speed;
        private float              maxDistance;
        private float              explosionTileSize = 1f;
        private bool               stopped = false;
        private CircleCollider2D   circleCollider;
        private const float FlyingRadius = 0.25f;

        public void Initialize(FireBossController bossRef, Vector3 direction, float moveSpeed,
                               float travelDistance, float cloudRadius, float tileSize)
        {
            spawnTime = Time.time;
            moveDirection = new Vector3(direction.x, direction.y, 0f).normalized;
            if (moveDirection.sqrMagnitude <= 0f)
                moveDirection = Vector3.up;

            startPosition = transform.position;
            speed = moveSpeed;
            maxDistance = travelDistance;
            radius = Mathf.Max(0.01f, cloudRadius);
            explosionTileSize = Mathf.Max(0.01f, tileSize);
            transform.localScale = Vector3.one * (FlyingRadius * 2f);

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sortingOrder = 5;
            }

            circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider == null)
                circleCollider = gameObject.AddComponent<CircleCollider2D>();

            if (circleCollider != null)
            {
                circleCollider.radius = 0.5f;
                circleCollider.isTrigger = true;
            }

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody2D>();

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        private void Update()
        {
            if (!stopped)
            {
                transform.position += moveDirection * speed * Time.deltaTime;

                if (Vector2.Distance(startPosition, transform.position) >= maxDistance)
                    Spread();
            }

            if (!exploded && Time.time - spawnTime >= lifetime)
                Expire();
        }

        public void Explode()
        {
            if (exploded) return;
            exploded = true;

            // ★ XZ 평면 기준 폭발 판정
            Vector2 boxSize = Vector2.one * (explosionTileSize * 2f);
            var hits = Physics2D.OverlapBoxAll(
                transform.position,
                boxSize,
                0f
            );
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    FireBossController.ApplyDamageToPlayer(hit.gameObject, explosionDamage);
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
            if (other.CompareTag("Player"))
            {
                if (!stopped)
                    Spread();
                return;
            }

            if (stopped && (other.CompareTag("Fireball") || other.CompareTag("FireRing")))
                Explode();
        }

        private void Spread()
        {
            stopped = true;
            transform.localScale = Vector3.one * (radius * 2f);

            if (circleCollider != null)
                circleCollider.radius = 0.5f;
        }
    }
}
