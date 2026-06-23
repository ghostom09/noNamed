using System.Collections.Generic;
using UnityEngine;

namespace BossSystem.Boss.FireBoss
{
    public class FireRing : MonoBehaviour
    {
        private float innerRadius;
        private float maxRadius;
        private float damage;
        private bool isGasTrigger;
        private bool isSolidCircle;
        private FireBossController boss;

        private float startTime;
        private float destroyTime;
        private bool started = false;
        private bool gasTriggered = false;

        private const float AttackDuration = 0.25f;

        private SpriteRenderer spriteRenderer;
        private LineRenderer ringRenderer;

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
            float innerR,
            float maxR,
            float dmg,
            float delay,
            bool gasTrigger,
            FireBossController bossRef,
            bool solidCircle = false)
        {
            innerRadius = Mathf.Max(0f, innerR);
            maxRadius = Mathf.Max(innerRadius, maxR);
            damage = dmg;
            isGasTrigger = gasTrigger;
            isSolidCircle = solidCircle;
            boss = bossRef;

            startTime = Time.time + delay;
            destroyTime = startTime + AttackDuration;

            started = false;
            gasTriggered = false;
            hitTargets.Clear();

            transform.localScale = Vector3.one;

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
                UpdateVisual();
            }

            CheckRingOverlap();

            if (!gasTriggered && isGasTrigger && boss != null)
            {
                gasTriggered = true;
                TriggerGasExplosionInArea();
            }

            if (Time.time >= destroyTime)
                Destroy(gameObject);
        }

        private void UpdateVisual()
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            if (ringRenderer == null)
                ringRenderer = gameObject.AddComponent<LineRenderer>();

            FireBossController.SetupRingLine(
                ringRenderer,
                isSolidCircle ? 0f : innerRadius,
                maxRadius,
                new Color(1f, 0.4f, 0f, 0.85f),
                4);
        }

        private void CheckRingOverlap()
        {
            Vector2 center = transform.position;
            float minRadius = isSolidCircle ? 0f : innerRadius;

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, maxRadius);

            foreach (Collider2D hit in hits)
            {
                if (hit.isTrigger)
                    continue;

                if (hitTargets.Contains(hit))
                    continue;

                if (!hit.CompareTag("Player"))
                    continue;

                float dist = Vector2.Distance(center, hit.transform.position);

                if (!isSolidCircle && dist < minRadius)
                    continue;

                if (dist > maxRadius)
                    continue;

                if (!FireBossController.ApplyDamageToPlayer(hit.gameObject, damage))
                    continue;

                hitTargets.Add(hit);
            }
        }

        private void TriggerGasExplosionInArea()
        {
            Vector2 center = transform.position;
            float minRadius = isSolidCircle ? 0f : innerRadius;

            foreach (GasCloud gas in boss.ActiveGasClouds)
            {
                if (gas == null || !gas.IsSpread)
                    continue;

                float dist = Vector2.Distance(center, gas.transform.position);
                if (dist + gas.Radius < minRadius)
                    continue;

                if (dist > maxRadius + gas.Radius)
                    continue;

                gas.Explode();
            }
        }
    }
}
