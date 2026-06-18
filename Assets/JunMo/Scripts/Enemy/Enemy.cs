using System;
using System.Collections;
using Unity.VisualScripting;
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
    public CCState CcState;
    public HitState HitState;
    
    public IMovement Movement;
    public IAttack Attack;
    public IChase Chase;
    
    public EnemyStats stats;
    
    [HideInInspector]public Rigidbody2D rb;
    private float _attackTime = 0f;
    private float explosionRadius = 3f;
    private bool _hasExploded = false;
    private GameObject _boom;
    
    public event Action<Enemy> OnDead;
    public bool IsAttacking { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        target = GameObject.Find("Player").transform;
        // OnDead?.Invoke(this);
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

    public void Explode()
    {
        if (_hasExploded)
            return;

        _hasExploded = true;
        StartCoroutine(Boom());
    }

    private IEnumerator Boom()
    {
        yield return new WaitForSeconds(0.5f);
        _boom = EnemyPrefabController.Instance.GetPrefab(stats.attackType);
        var boom = Instantiate(_boom, transform.position, Quaternion.identity);
        Destroy(boom, 1f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // if (hit.gameObject.TryGetComponent<IDamageable>(out var damageable))
                // {
                //     damageable.TakeDamage(stats.damage);
                //     Debug.Log($"자폭 데미지{stats.damage}");
                // }
            }
        }
        StopCoroutine(Boom());
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector2 baseDir = GetVector2();
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        Vector3 prevPoint = transform.position;

        for (int i = 0; i < 10; i++)
        {
            float angle = baseAngle - 120f / 2f + (120f / 10f) * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 point = transform.position +
                            new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * 2f;

            Gizmos.DrawLine(transform.position, point);

            if (i > 0)
                Gizmos.DrawLine(prevPoint, point);

            prevPoint = point;
        }
    }
}
