using UnityEngine;
using System;

namespace BossSystem.Boss.FleshBoss
{
    /// <summary>
    /// 살점 오브젝트
    /// ─ 파괴 가능한 오브젝트 (체력 있음)
    /// ─ 플레이어가 닿으면 데미지
    /// ─ 패턴 5: 보스가 흡수하면 체력 회복
    /// ─ 패턴 3: BounceCount 설정 시 튕기다 사라짐
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class FleshChunk : MonoBehaviour
    {
        [Header("기본 설정")]
        [SerializeField] private float chunkHP       = 30f;
        [SerializeField] private float touchDamage   = 15f;
        [SerializeField] private float lifetime      = 10f;   // 최대 생존 시간

        // 튕기기 설정 (패턴 3 전용)
        private int maxBounces    = 0;
        private int currentBounce = 0;
        private bool isBouncing   = false;

        // 컴포넌트
        private Rigidbody2D rb;
        private bool isDead = false;
        private bool isTargetedFlight = false;
        private Vector2 flightTargetPos;
        private float flightSpeed = 0f;
        private const float FlightReachThreshold = 0.03f;

        // 이벤트
        public Action<FleshChunk> OnDestroyed;   // 보스가 구독해 목록 관리
        public Action<FleshChunk> OnAbsorbed;    // 패턴 5 흡수 시 보스 호출

        // 보스 참조 (자기 자신을 생성한 보스)
        private FleshBossController owner;

        // ── 초기화 ──────────────────────────────────────────────
        public void Initialize(FleshBossController boss, float hp = 30f, float dmg = 15f,
                               float life = 10f, int bounces = 0)
        {
            owner         = boss;
            chunkHP       = hp;
            touchDamage   = dmg;
            lifetime      = life;
            maxBounces    = bounces;
            isBouncing    = bounces > 0;
            rb            = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            Destroy(gameObject, lifetime);
        }

        public void InitializeTargetedFlight(Vector3 targetPosition, float speed)
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();

            flightTargetPos = new Vector2(targetPosition.x, targetPosition.y);
            flightSpeed = Mathf.Max(0.01f, speed);
            isTargetedFlight = true;
            isBouncing = false;
            currentBounce = 0;

            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        private void FixedUpdate()
        {
            if (!isTargetedFlight || isDead || rb == null) return;

            Vector2 next = Vector2.MoveTowards(
                rb.position,
                flightTargetPos,
                flightSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);

            if (Vector2.Distance(next, flightTargetPos) <= FlightReachThreshold)
            {
                rb.MovePosition(flightTargetPos);
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                isTargetedFlight = false;
            }
        }

        // ── 물리 충돌 ────────────────────────────────────────────
        private void OnCollisionEnter2D(Collision2D col)
        {
            // 플레이어에게 데미지
            if (col.gameObject.CompareTag("Player"))
            {
                // col.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(touchDamage);
            }

            // 튕기기 처리 (패턴 3)
            if (isBouncing)
            {
                currentBounce++;
                if (currentBounce >= maxBounces)
                {
                    DestroyChunk();
                }
                // 실제 튕김은 Rigidbody2D sharedMaterial의 bounciness로 처리
                // 또는 여기서 직접 반사벡터 계산 가능
            }
        }

        // 트리거용 (장판 등)
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                
            }
                // other.GetComponent<PlayerHealth>()?.TakeDamage(touchDamage);
        }

        // ── 피해 수신 ────────────────────────────────────────────
        public void TakeDamage(float amount)
        {
            if (isDead) return;
            chunkHP -= amount;
            if (chunkHP <= 0f) DestroyChunk();
        }

        // ── 보스 흡수 (패턴 5) ──────────────────────────────────
        public void AbsorbByBoss()
        {
            if (isDead) return;
            isDead = true;
            OnAbsorbed?.Invoke(this);
            Destroy(gameObject);
        }

        // ── 파괴 ────────────────────────────────────────────────
        private void DestroyChunk()
        {
            if (isDead) return;
            isDead = true;
            OnDestroyed?.Invoke(this);
            // TODO: 파괴 VFX 스폰
            Destroy(gameObject);
        }

        public float TouchDamage => touchDamage;
        public bool  IsDead      => isDead;
    }

    // ── 간단한 PlayerHealth 참조용 (FireProjectiles.cs의 것과 동일 namespace 충돌 방지) ──
    // 실제 프로젝트에서는 공용 namespace의 PlayerHealth를 사용하세요.
}
