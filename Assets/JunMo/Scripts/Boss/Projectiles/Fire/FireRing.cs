using System.Collections.Generic;
using UnityEngine;

namespace BossSystem.Boss.FireBoss
{
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
}