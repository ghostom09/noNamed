using UnityEngine;
using BossSystem.BehaviorTree;
using BossSystem;
using BossSystem.Scripable;

namespace BossSystem.Boss
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class BossBase : MonoBehaviour
    {
        [Header("기본 스탯")]
        [SerializeField] protected float maxHP     = 1000f;
        [SerializeField] protected float moveSpeed = 3f;

        [Header("ScriptableObject 오버라이드 (선택)")]
        [SerializeField] protected BossAttackData bossData;

        [Header("Telegraph Prefabs")]
        [SerializeField] protected GameObject circleTelegraphPrefab;
        [SerializeField] protected GameObject lineTelegraphPrefab;
        [SerializeField] protected GameObject sectorTelegraphPrefab;
        [SerializeField] protected GameObject squareTelegraphPrefab;

        [Header("참조")]
        [SerializeField] protected Transform player;

        protected BossBlackboard blackboard;
        protected BTNode         behaviorTree;
        protected Rigidbody2D    rb;

        public float CurrentHP => currentHP;
        public bool  IsDead    => currentHP <= 0f;

        // ── 패턴 실행 플래그 ──────────────────────────────────────
        public bool IsExecutingPattern { get; private set; } = false;
        public bool IsTelegraphing     { get; private set; } = false;

        /// <summary>
        /// 안전장치: 이 시간(초)이 지나도 패턴이 끝나지 않으면 강제 해제
        /// 기본 20초. 가장 긴 패턴보다 넉넉하게 설정.
        /// </summary>
        [Header("패턴 안전장치")]
        [SerializeField] private float patternTimeoutSec = 20f;
        private float patternStartTime = 0f;

        public System.Action OnPhase2Enter;
        public System.Action OnDeath;

        protected float currentHP;
        private   bool  phase2Triggered = false;
        private readonly TelegraphMaker telegraphMaker = new TelegraphMaker();

        // ── 초기화 ────────────────────────────────────────────────
        protected virtual void Awake()
        {
            if (bossData != null)
            {
                if (bossData.overrideMaxHP     > 0f) maxHP     = bossData.overrideMaxHP;
                if (bossData.overrideMoveSpeed > 0f) moveSpeed = bossData.overrideMoveSpeed;
            }

            currentHP = maxHP;
            rb        = GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.gravityScale   = 0f;
                rb.freezeRotation = true;
            }

            blackboard = new BossBlackboard
            {
                BossTransform = transform,
                Boss          = this,
                CurrentHP     = currentHP,
                MaxHP         = maxHP
            };
        }

        protected virtual void Start()
        {
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
            }

            blackboard.PlayerTransform = player;
            behaviorTree = BuildBehaviorTree();
        }

        // ── 매 프레임 ─────────────────────────────────────────────
        // BossBase.cs
        protected virtual void FixedUpdate()          // Update → FixedUpdate
        {
            if (IsDead) return;

            blackboard.CurrentHP = currentHP;

            if (IsExecutingPattern &&
                Time.time - patternStartTime > patternTimeoutSec)
            {
                Debug.LogWarning($"[{gameObject.name}] 패턴 타임아웃 — 강제 해제");
                ForceReleasePattern();
            }

            behaviorTree?.Evaluate();

            if (!IsTelegraphing && !IsExecutingPattern)
                ChasePlayer();
        }

        protected virtual void Update()
        {
            if (IsDead) return;
            if (!phase2Triggered && blackboard.IsPhase2)
            {
                phase2Triggered = true;
                OnEnterPhase2();
                OnPhase2Enter?.Invoke();
            }
        }

        // ── 추상/가상 ─────────────────────────────────────────────
        protected abstract BTNode BuildBehaviorTree();

        protected virtual void OnEnterPhase2()
            => Debug.Log($"[{gameObject.name}] Phase 2 진입!");

        protected virtual bool  ShouldChasePlayer     => true;
        protected virtual float ChaseSpeed            => moveSpeed;
        protected virtual float ChaseStoppingDistance => 1.5f;

        // ── 패턴 플래그 API ───────────────────────────────────────
        /// <summary>패턴 시작 시 true, 정상 종료 시 false 호출.</summary>
        public void SetExecutingPattern(bool value)
        {
            IsExecutingPattern = value;
            if (value)
                patternStartTime = Time.time;
        }

        /// <summary>텔레그래프 진행 중 이동 정지.</summary>
        public void SetTelegraphing(bool value)
        {
            IsTelegraphing = value;
            if (value && rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        /// <summary>
        /// 강제 패턴 해제 — 타임아웃 또는 예외 경로에서 호출.
        /// 모든 플래그를 안전하게 초기화.
        /// </summary>
        public void ForceReleasePattern()
        {
            IsExecutingPattern = false;
            IsTelegraphing     = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        public virtual void TakeDamage(float damage)
        {
            if (IsDead) return;
            currentHP = Mathf.Max(0f, currentHP - damage);
            blackboard.CurrentHP = currentHP;    // ← 즉시 동기화 추가
            if (currentHP <= 0f) { OnDeath?.Invoke(); OnDie(); }
        }

        protected virtual void OnDie()
        {
            ForceReleasePattern();
            Debug.Log($"[{gameObject.name}] 사망");
            Destroy(gameObject, 1f);
        }

        // ── 이동 ──────────────────────────────────────────────────
        public Transform GetPlayer() => player;

        public Telegraph SpawnTelegraph(
            BossAttackData data,
            TelegraphShape shape,
            float radius,
            Vector2 direction = default,
            bool followBoss = true,
            System.Action onComplete = null,
            float duration = -1f)
        {
            Telegraph telegraph = telegraphMaker.SpawnCircle(
                GetTelegraphPrefab(shape),
                transform.position,
                radius,
                data,
                onComplete,
                followBoss ? transform : null,
                duration);

            RotateTelegraphToDirection(telegraph, direction);
            return telegraph;
        }

        public Telegraph SpawnTelegraphAt(
            Vector3 position,
            BossAttackData data,
            TelegraphShape shape,
            float radius,
            Vector2 direction = default,
            System.Action onComplete = null,
            float duration = -1f)
        {
            Telegraph telegraph = telegraphMaker.SpawnCircle(
                GetTelegraphPrefab(shape),
                position,
                radius,
                data,
                onComplete,
                null,
                duration);

            RotateTelegraphToDirection(telegraph, direction);
            return telegraph;
        }

        public Telegraph SpawnLineTelegraph(
            Vector3 startPosition,
            Vector2 direction,
            float length,
            float width,
            BossAttackData data,
            System.Action onComplete = null,
            float duration = -1f,
            bool anchorAtStart = false)
        {
            return telegraphMaker.SpawnLine(
                lineTelegraphPrefab != null ? lineTelegraphPrefab : circleTelegraphPrefab,
                startPosition,
                direction,
                length,
                width,
                data,
                onComplete,
                duration,
                anchorAtStart);
        }

        protected GameObject GetTelegraphPrefab(TelegraphShape shape)
        {
            switch (shape)
            {
                case TelegraphShape.Line:
                    return lineTelegraphPrefab != null ? lineTelegraphPrefab : circleTelegraphPrefab;
                case TelegraphShape.Sector:
                    return sectorTelegraphPrefab != null ? sectorTelegraphPrefab : circleTelegraphPrefab;
                case TelegraphShape.Square:
                    return squareTelegraphPrefab != null ? squareTelegraphPrefab : circleTelegraphPrefab;
                default:
                    return circleTelegraphPrefab;
            }
        }

        private static void RotateTelegraphToDirection(Telegraph telegraph, Vector2 direction)
        {
            if (telegraph == null || direction.sqrMagnitude <= 0f)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            telegraph.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        protected void ChasePlayer()
        {
            if (!ShouldChasePlayer || player == null || rb == null) return;
            if (blackboard.DistanceToPlayer <= ChaseStoppingDistance) return;
            MoveToward(player.position, ChaseSpeed);
        }

        protected void MoveToward(Vector3 target, float speed)
        {
            Vector2 bossPos   = rb.position;                              // ★
            Vector2 targetPos = new Vector2(target.x, target.y);         // ★
            Vector2 dir       = targetPos - bossPos;
            if (dir.sqrMagnitude < 0.01f) return;
            rb.MovePosition(bossPos + dir.normalized * speed * Time.fixedDeltaTime); // ★
        }
    }
}
