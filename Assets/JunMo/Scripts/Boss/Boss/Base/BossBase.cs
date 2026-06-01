using UnityEngine;
using BossSystem.BehaviorTree;

namespace BossSystem.Boss
{
    /// <summary>
    /// 탑다운 2D 보스 베이스
    ///
    /// 수정 사항 (탑다운 대응):
    ///  - Collider2D/Rigidbody2D 기반
    ///  - LookAtPlayer: XY 평면에서 Z축 회전
    ///  - BuildBehaviorTree()를 Start()로 이동 → Player 참조 null 방지
    ///  - Rigidbody2D는 중력 0, 회전 고정
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class BossBase : MonoBehaviour
    {
        [Header("기본 스탯")]
        [SerializeField] protected float maxHP    = 1000f;
        [SerializeField] protected float moveSpeed = 3f;

        [Header("참조")]
        [SerializeField] protected Transform player;

        protected BossBlackboard blackboard;
        protected BTNode         behaviorTree;
        protected Rigidbody2D    rb;

        public float CurrentHP => currentHP;
        public bool  IsDead    => currentHP <= 0f;

        public System.Action OnPhase2Enter;
        public System.Action OnDeath;

        protected float currentHP;
        private bool    phase2Triggered = false;

        // ── 초기화 ────────────────────────────────────────────────
        protected virtual void Awake()
        {
            currentHP = maxHP;
            rb        = GetComponent<Rigidbody2D>();

            // ★ 탑다운: Y축 이동·회전 잠금
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
            }

            // 블랙보드 생성 (Player는 Start에서 채움)
            blackboard = new BossBlackboard
            {
                BossTransform = transform,
                Boss          = this,
                CurrentHP     = currentHP,
                MaxHP         = maxHP
            };
        }

        /// <summary>
        /// ★ BuildBehaviorTree를 Start()에서 호출
        ///    Awake() 시점에는 Player가 아직 null일 수 있음
        /// </summary>
        protected virtual void Start()
        {
            // 플레이어 자동 탐색
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
            }

            blackboard.PlayerTransform = player;
            behaviorTree = BuildBehaviorTree();    // ★ Awake → Start 이동
        }

        // ── 매 프레임 ─────────────────────────────────────────────
        protected virtual void Update()
        {
            if (IsDead) return;

            // 블랙보드 HP 동기화
            blackboard.CurrentHP = currentHP;

            // 페이즈2 전환
            if (!phase2Triggered && blackboard.IsPhase2)
            {
                phase2Triggered = true;
                OnEnterPhase2();
                OnPhase2Enter?.Invoke();
            }

            // 비헤이비어 트리 평가
            behaviorTree?.Evaluate();
            ChasePlayer();
        }

        // ── 추상 메서드 ───────────────────────────────────────────
        protected abstract BTNode BuildBehaviorTree();

        // ── 가상 메서드 ───────────────────────────────────────────
        protected virtual void OnEnterPhase2()
            => Debug.Log($"[{gameObject.name}] Phase 2 진입!");

        protected virtual bool ShouldChasePlayer => true;
        protected virtual float ChaseSpeed => moveSpeed;
        protected virtual float ChaseStoppingDistance => 1.5f;

        // ── 공통 인터페이스 ───────────────────────────────────────
        public virtual void TakeDamage(float damage)
        {
            if (IsDead) return;
            currentHP = Mathf.Max(0f, currentHP - damage);
            if (currentHP <= 0f) { OnDeath?.Invoke(); OnDie(); }
        }

        protected virtual void OnDie()
        {
            Debug.Log($"[{gameObject.name}] 사망");
            Destroy(gameObject, 1f);
        }

        // ── 유틸리티 ─────────────────────────────────────────────
        public Transform GetPlayer() => player;

        protected void ChasePlayer()
        {
            if (!ShouldChasePlayer || player == null || rb == null) return;
            if (blackboard.DistanceToPlayer <= ChaseStoppingDistance) return;

            MoveToward(player.position, ChaseSpeed);
        }

        /// <summary>탑다운 이동 (Rigidbody2D.MovePosition 사용)</summary>
        protected void MoveToward(Vector3 target, float speed)
        {
            Vector3 dir = target - transform.position;
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.01f) return;

            Vector3 next = transform.position + speed * Time.deltaTime * dir.normalized;
            next.z = transform.position.z;
            rb.MovePosition(next);
        }
    }
}
