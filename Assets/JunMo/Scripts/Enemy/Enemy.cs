using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField]private Transform target;
    private IState _currentState;
    
    public IdleState IdleState;
    public MoveState MoveState;
    public AttackState AttackState;
    public ChaseState ChaseState;
    public DieState DieState;
    public EnemyStats stats;
    public CCState CcState;
    public HitState HitState;
    
    public IMovement Movement;
    public IAttack Attack;
    public IChase Chase;
    
    [HideInInspector]public Rigidbody2D rb;
    private float _attackTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        IdleState = new IdleState(this);
        MoveState = new MoveState(this);
        AttackState = new AttackState(this);
        ChaseState = new ChaseState(this);
        DieState = new DieState(this);
        CcState = new CCState(this);
        
        EnemyFactory.Initialize(this, stats);
        
        ChangeState(IdleState);
    }

    private void Update()
    {
        _attackTime += Time.deltaTime;
    
        _currentState?.Update();
    }
    
    public void ChangeState(IState newState)
    {
        _currentState?.Exit();

        _currentState = newState;

        _currentState.Enter();
    }
    
    public bool CanAttackRange()
    {
        return CanRange(stats.attackRange);
    }

    public bool CanChaseRange()
    {
        return CanRange(stats.chaseRange);
    }
    
    public bool CanAttackSpeed()
    {
        return _attackTime >= stats.attackSpeed;
    }

    private bool CanRange(float range) // 근접 공격범위 안인가?
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
                return true;
        }
        return false;
    }

    public Vector2 GetVector2() // 방향
    {
        Vector2 dir = (Vector2)target.position - (Vector2)transform.position;
        return dir.normalized;
    }

    public Vector2 GetPlayerVector2() // 위치
    {
        return target.position;
    }
    
    public float GetDistanceToPlayer() // 거리
    {
        return Vector2.Distance((Vector2)transform.position, (Vector2)target.position);
    }

    public Transform GetTransform()
    {
        return target.transform;
    }
    
    public void ResetAttackTimer()
    {
        _attackTime = 0f;
    }
    
    public void ApplyStun(float duration) => CcState.ApplyStun(duration);
    public void ApplySnare(float duration) => CcState.ApplySnare(duration);
    public void ApplyKnockBack(Vector2 hitDir, float force, float duration) 
        => CcState.ApplyKnockback(hitDir, force, duration);
}
