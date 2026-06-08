using System.Collections.Generic;
using UnityEngine;

namespace BossSystem.Boss.FireBoss
{
    /// <summary>
    /// 불의 고리 (SpriteRenderer 버전)
    /// </summary>
    public class FireRing : MonoBehaviour
    {
        private float expandSpeed;
        private float maxRadius;
        private float damage;
        private bool isGasTrigger;
        private FireBossController boss;

        private float currentRadius = 0f;
        private float startTime;
        private bool started = false;

        // 링의 충돌 판정 두께
        private float thickness = 0.8f;

        private SpriteRenderer spriteRenderer;

        private readonly HashSet<Collider2D> hitTargets = new();

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.4f, 0f, 0.85f);
                spriteRenderer.sortingOrder = 4;
            }
        }

        public void Initialize(
            float spd,
            float maxR,
            float dmg,
            float delay,
            bool gasTrigger,
            FireBossController bossRef)
        {
            expandSpeed = spd;
            maxRadius = maxR;
            damage = dmg;
            isGasTrigger = gasTrigger;
            boss = bossRef;

            startTime = Time.time + delay;

            currentRadius = 0f;
            started = false;

            hitTargets.Clear();

            transform.localScale = Vector3.zero;

            if (spriteRenderer != null)
                spriteRenderer.enabled = false;
        }

        private void Update()
        {
            if (!started)
            {
                if (Time.time < startTime)
                    return;

                started = true;

                if (spriteRenderer != null)
                    spriteRenderer.enabled = true;
            }

            currentRadius += expandSpeed * Time.deltaTime;
            currentRadius = Mathf.Min(currentRadius, maxRadius);

            UpdateVisual();

            CheckRingOverlap();

            if (currentRadius >= maxRadius)
            {
                if (isGasTrigger && boss != null)
                    boss.TriggerGasExplosion(transform.position, currentRadius);

                Destroy(gameObject);
            }
        }

        private void UpdateVisual()
        {
            // 기본 스프라이트가 지름 1 유닛이라고 가정
            float diameter = currentRadius * 2f;

            transform.localScale = new Vector3(
                diameter,
                diameter,
                1f
            );
        }

        private void CheckRingOverlap()
        {
            Vector2 center = transform.position;

            float outerRadius = currentRadius + thickness * 0.5f;
            float innerRadius = Mathf.Max(0f, currentRadius - thickness * 0.5f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, outerRadius);

            foreach (Collider2D hit in hits)
            {
                if (hit.isTrigger)
                    continue;

                if (hitTargets.Contains(hit))
                    continue;

                if (!hit.CompareTag("Player"))
                    continue;

                float dist = Vector2.Distance(
                    center,
                    hit.transform.position
                );

                if (dist < innerRadius || dist > outerRadius)
                    continue;

                hitTargets.Add(hit);

                PlayerHealth health = hit.GetComponent<PlayerHealth>();
                if (health != null)
                    health.TakeDamage(damage);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;

            float outerRadius = currentRadius + thickness * 0.5f;
            float innerRadius = Mathf.Max(0f, currentRadius - thickness * 0.5f);

            Gizmos.DrawWireSphere(transform.position, outerRadius);
            Gizmos.DrawWireSphere(transform.position, innerRadius);
        }
#endif
    }
}