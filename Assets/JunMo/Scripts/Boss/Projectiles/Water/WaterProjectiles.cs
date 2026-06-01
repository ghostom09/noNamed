using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    // ═══════════════════════════════════════════════════════════════
    //  물기둥
    //  지정 위치에 낙하 → 피해 + 속박 적용 → bindDuration 유지 후 소멸
    // ═══════════════════════════════════════════════════════════════
    public class WaterPillar : MonoBehaviour
    {
        private float radius;
        private float damage;
        private float bindDuration;

        public void Initialize(float r, float dmg, float bindDur)
        {
            radius       = r;
            damage       = dmg;
            bindDuration = bindDur;
            StartCoroutine(PillarLifecycle());
        }

        private IEnumerator PillarLifecycle()
        {
            // 낙하 연출: 위에서 아래로 스케일 확장
            float dropTime = 0.2f;
            float elapsed  = 0f;
            transform.localScale = new Vector3(1f, 0f, 1f);

            while (elapsed < dropTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dropTime;
                transform.localScale = new Vector3(1f, Mathf.Lerp(0f, 1f, t), 1f);
                yield return null;
            }
            transform.localScale = Vector3.one;

            // 피해 + 속박 판정
            var hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;
                hit.GetComponent<PlayerHealth>()?.TakeDamage(damage);
                hit.GetComponent<PlayerMovement>()?.ApplyBind(bindDuration);
            }

            // bindDuration 유지
            yield return new WaitForSeconds(bindDuration);

            // 페이드아웃
            float fadeTime = 0.35f;
            elapsed = 0f;
            var renderers = GetComponentsInChildren<Renderer>();
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeTime);
                foreach (var rend in renderers)
                {
                    var c = rend.material.color;
                    rend.material.color = new Color(c.r, c.g, c.b, alpha);
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  십자 빔 세그먼트 (4개로 + 형태 구성)
    //  활성화 중 트리거 안 플레이어에게 DPS 적용
    //  WaterBossController의 beamPivot 자식으로 배치:
    //    세그먼트 0°, 90°, 180°, 270° 각도로 배치 → + 형태
    // ═══════════════════════════════════════════════════════════════
    public class WaterBeamSegment : MonoBehaviour
    {
        private float dps;
        private float tickInterval = 0.1f;
        private float lastTick     = 0f;

        public void Initialize(float damagePerSec)
        {
            dps = damagePerSec;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (Time.time - lastTick < tickInterval) return;
            lastTick = Time.time;
            other.GetComponent<PlayerHealth>()?.TakeDamage(dps * tickInterval);
        }
    }

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

    // ═══════════════════════════════════════════════════════════════
    //  PlayerMovement 인터페이스 (프로젝트 컴포넌트로 교체)
    // ═══════════════════════════════════════════════════════════════
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 5f;
        private bool  isBound     = false;
        private float bindEnd     = 0f;
        private float slowEnd     = 0f;
        private float slowMult    = 1f;

        private void Update()
        {
            if (isBound && Time.time >= bindEnd)
            {
                isBound = false;
                Debug.Log("[Player] 속박 해제");
            }
            if (slowMult < 1f && Time.time >= slowEnd)
            {
                slowMult = 1f;
                Debug.Log("[Player] 둔화 해제");
            }
        }

        public void ApplyBind(float duration)
        {
            isBound = true;
            bindEnd = Time.time + duration;
            Debug.Log($"[Player] 속박 {duration}s");
        }

        public void ApplySlow(float multiplier, float duration)
        {
            slowMult = Mathf.Min(slowMult, multiplier);   // 더 강한 둔화 우선
            slowEnd  = Mathf.Max(slowEnd, Time.time + duration);
        }

        public void RemoveSlow() => slowMult = 1f;

        public bool  IsBound      => isBound;
        public float SpeedMult    => isBound ? 0f : slowMult;
        public float CurrentSpeed => baseSpeed * SpeedMult;
    }

    // PlayerHealth placeholder
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHP = 200f;
        private float hp;
        private void Awake() => hp = maxHP;
        public void TakeDamage(float dmg)
        {
            hp -= dmg;
            Debug.Log($"[Player] HP {hp:F0}/{maxHP} (-{dmg:F0})");
        }
    }
}
