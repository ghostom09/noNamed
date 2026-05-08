using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField]private Transform target;
    private IState _currentState;
    
    public IdleState IdleState;
    public MoveState MoveState;
    public AttackState AttackState;
    public EnemyStats stats;
    
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
        
        switch(stats.enemyType)
        {
            case EnemyType.Melee:
                Movement = new MeleeMovement(this);
                Chase = new MeleeChase(this);
                break;
        }
        
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

    public float DistanceToTarget()
    {
        return Vector3.Distance(transform.position, target.position);
    }

    public bool CanChaseRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, stats.chaseRange);

        Debug.Log($"Detected Count: {hits.Length}");

        foreach (var hit in hits)
        {
            Debug.Log($"Hit: {hit.name}");

            if (hit.CompareTag("Player"))
            {
                Debug.Log("PLAYER FOUND");
                return true;
            }
        }

        return false;
    }
    
    public bool CanAttackSpeed()
    {
        return _attackTime >= stats.attackSpeed;
    }

    public bool CanAttackRange()
    {
        return DistanceToTarget() >= stats.attackRange;
    }

    public float GetAngle()
    {
        Vector2 dir = new Vector3(target.position.x, target.position.y, transform.position.z) - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        return angle;
    }
    
    public void ResetAttackTimer()
    {
        _attackTime = 0f;
    }
}
