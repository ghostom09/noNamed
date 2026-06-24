using UnityEngine;
using System;
using BossSystem.Scripable;

namespace BossSystem.Boss.FleshBoss
{
    /// <summary>
    /// 살점 오브젝트
    /// ─ 파괴 가능한 오브젝트 (체력 있음)
    /// ─ 플레이어가 닿으면 데미지
    /// ─ 패턴 5: 보스가 흡수하면 체력 회복
    /// ─ 패턴 3: BounceCount 설정 시, 도착 후 같은 방향으로 N번 더 튕기며 회전 이동
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class FleshChunk : MonoBehaviour
    {
        [Header("기본 설정")]
        [SerializeField] private float chunkHP       = 30f;
        [SerializeField] private float touchDamage   = 15f;
        [SerializeField] private float lifetime      = 10f;   // 최대 생존 시간

        [Header("튕기기 설정")]
        [SerializeField] private float bounceDistance         = 3f;
        [SerializeField] private float bounceMoveSpeed        = 10f;
        [SerializeField] private float bounceRotationSpeed    = 360f; // 초당 회전 각도
        [SerializeField] private float bounceTelegraphRadius   = 1f;
        [SerializeField] private float bounceTelegraphLead     = 0.3f; // 텔레그래프 선딜레이
        [SerializeField] private float bounceTelegraphDuration = 0.3f; // 텔레그래프 차는 시간

        // 튕기기 상태 (패턴 3 전용)
        private int     maxBounces    = 0;
        private int     currentBounce = 0;
        private Vector2 flightDir;          // 날아간 방향 (튕길 때도 유지)

        private enum BounceState { None, Telegraph, Moving }
        private BounceState bounceState = BounceState.None;
        private Vector2 bounceStartPos;
        private Vector2 bounceTargetPos;

        // 컴포넌트
        private Rigidbody2D rb;
        private bool isDead = false;
        private Vector2 spawnPosition;
        private float maxTravelRange = 0f;
        private bool stopAtMaxTravelRange = false;
        private bool hasReachedMaxTravelRange = false;
        private bool isTargetedFlight = false;
        private Vector2 flightTargetPos;
        private float flightSpeed = 0f;
        private const float FlightReachThreshold = 0.03f;

        // 텔레그래프용 데이터
        private BossAttackData bounceTelegraphData;

        // 이벤트
        public Action<FleshChunk> OnDestroyed;   // 보스가 구독해 목록 관리
        public Action<FleshChunk> OnAbsorbed;    // 패턴 5 흡수 시 보스 호출

        // 보스 참조 (자기 자신을 생성한 보스)
        private FleshBossController owner;

        // ── 초기화 ──────────────────────────────────────────────
        public void Initialize(FleshBossController boss, float hp = 30f, float dmg = 15f,
                               float life = 10f, int bounces = 0,
                               BossAttackData bounceTelegraph = null,
                               float maxRange = 0f,
                               bool stopAtMaxRange = false)
        {
            owner               = boss;
            chunkHP             = hp;
            touchDamage         = dmg;
            lifetime            = life;
            maxBounces          = bounces;
            currentBounce       = 0;
            bounceState         = BounceState.None;
            bounceTelegraphData = bounceTelegraph;
            rb                  = GetComponent<Rigidbody2D>();
            rb.gravityScale     = 0f;
            spawnPosition       = rb.position;
            maxTravelRange      = maxRange;
            stopAtMaxTravelRange = stopAtMaxRange;
            hasReachedMaxTravelRange = false;

            Destroy(gameObject, lifetime);
        }

        public void InitializeTargetedFlight(Vector3 targetPosition, float speed)
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();

            Vector2 start   = rb.position;
            flightTargetPos = new Vector2(targetPosition.x, targetPosition.y);
            flightSpeed     = Mathf.Max(0.01f, speed);
            isTargetedFlight = true;

            Vector2 toTarget = flightTargetPos - start;
            flightDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;

            rb.gravityScale    = 0f;
            rb.linearVelocity  = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        private void FixedUpdate()
        {
            if (isDead || rb == null) return;

            if (!hasReachedMaxTravelRange && maxTravelRange > 0f &&
                Vector2.Distance(spawnPosition, rb.position) >= maxTravelRange)
            {
                if (stopAtMaxTravelRange)
                {
                    hasReachedMaxTravelRange = true;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                else
                {
                    DestroyChunk();
                    return;
                }
            }

            if (isTargetedFlight)
            {
                Vector2 next = Vector2.MoveTowards(
                    rb.position,
                    flightTargetPos,
                    flightSpeed * Time.fixedDeltaTime);
                rb.MovePosition(next);

                if (Vector2.Distance(next, flightTargetPos) <= FlightReachThreshold)
                {
                    rb.MovePosition(flightTargetPos);
                    rb.linearVelocity  = Vector2.zero;
                    rb.angularVelocity = 0f;
                    isTargetedFlight = false;

                    if (maxBounces > 0)
                        StartNextBounce();
                }
                return;
            }

            UpdateBounce();
        }

        // ── 튕기기 (도착 후 같은 방향으로 N번 추가 이동) ──────────
        private void StartNextBounce()
        {
            if (currentBounce >= maxBounces)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            bounceStartPos  = rb.position;
            bounceTargetPos = bounceStartPos + flightDir * bounceDistance;

            SpawnBounceTelegraph(bounceTargetPos);
            bounceState = BounceState.Telegraph;

            Invoke(nameof(BeginBounceMove), bounceTelegraphLead);
        }

        private void BeginBounceMove()
        {
            if (isDead) return;
            bounceState = BounceState.Moving;
        }

        private void UpdateBounce()
        {
            if (bounceState != BounceState.Moving) return;

            Vector2 next = Vector2.MoveTowards(
                rb.position,
                bounceTargetPos,
                bounceMoveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);
            rb.MoveRotation(rb.rotation + bounceRotationSpeed * Time.fixedDeltaTime);

            if (Vector2.Distance(next, bounceTargetPos) <= FlightReachThreshold)
            {
                rb.MovePosition(bounceTargetPos);
                bounceState = BounceState.None;
                currentBounce++;

                if (currentBounce >= maxBounces)
                {
                    rb.linearVelocity = Vector2.zero;
                    DestroyChunk();
                    // 다 튕겨도 파괴하지 않음 (lifetime에 의해서만 소멸)
                }
                else
                {
                    StartNextBounce();
                }
            }
        }

        // ── 물리 충돌 ────────────────────────────────────────────
        private void OnCollisionEnter2D(Collision2D col)
        {
            BossDamageUtility.TryDamagePlayer(col.collider, touchDamage);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            BossDamageUtility.TryDamagePlayer(other, touchDamage);
        }

        // ── 튕길 위치 텔레그래프 ──────────────────────────────────
        private void SpawnBounceTelegraph(Vector2 position)
        {
            if (owner == null || bounceTelegraphData == null) return;

            owner.SpawnTelegraphAt(
                position,
                bounceTelegraphData,
                TelegraphShape.Circle,
                radius: bounceTelegraphRadius,
                duration: bounceTelegraphDuration);
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
}
