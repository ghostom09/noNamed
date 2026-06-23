using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class JunMoEnemyCombatAdapter : MonoBehaviour, IDamageable, IHitPointStatus, ISlowable, IPoisonable, IStunnable, IBindable, IKnockbackable
{
    [SerializeField] private Enemy enemy;
    [SerializeField] private Rigidbody2D targetRigidbody;

    private Coroutine _poisonCoroutine;
    private Coroutine _slowCoroutine;
    private float _currentHp;
    private float _baseMoveSpeed;
    private bool _hasMoveSpeedSnapshot;
    private bool _hasTakenDamage;
    private bool _notifiedDeath;

    private static readonly FieldInfo EnemyOnDeadField = typeof(Enemy).GetField(
        "OnDead",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public float CurrentHp => _currentHp;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        BindReferences();
        InitializeHitPointsIfNeeded();
    }

    private void Start()
    {
        BindReferences();
        if (!_hasTakenDamage)
            InitializeHitPointsFromCurrentStats();
        else
            InitializeHitPointsIfNeeded();

        CaptureMoveSpeedIfNeeded();
    }

    private void OnEnable()
    {
        BindReferences();

        if (enemy != null)
            enemy.OnDead += HandleEnemyDead;
    }

    private void OnDisable()
    {
        if (enemy != null)
            enemy.OnDead -= HandleEnemyDead;

        RestoreMoveSpeed();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        InitializeHitPointsIfNeeded();
        _hasTakenDamage = true;

        float hpBefore = _currentHp;
        _currentHp = Mathf.Max(0f, _currentHp - Mathf.Max(0f, amount));

        Debug.Log($"[Enemy Hit] {gameObject.name} damage:{amount:0.##} hp:{hpBefore:0.##}->{_currentHp:0.##}");

        if (_currentHp <= 0f)
        {
            Die();
        }
    }

    public void ApplySlow(float multiplier, float duration)
    {
        if (_slowCoroutine != null)
            StopCoroutine(_slowCoroutine);

        _slowCoroutine = StartCoroutine(SlowRoutine(Mathf.Clamp01(multiplier), duration));
    }

    public void ApplyPoison(float damagePerTick, float duration, float tickInterval)
    {
        if (_poisonCoroutine != null)
            StopCoroutine(_poisonCoroutine);

        _poisonCoroutine = StartCoroutine(PoisonRoutine(damagePerTick, duration, tickInterval));
    }

    public void ApplyStun(float duration)
    {
        enemy?.ApplyStun(duration);
    }

    public void ApplyBind(float duration, float damagePerTick, float tickInterval)
    {
        enemy?.ApplySnare(duration);

        if (damagePerTick > 0f && tickInterval > 0f)
            ApplyPoison(damagePerTick, duration, tickInterval);
    }

    public void ApplyKnockback(Vector2 direction, float impulse, float collisionDamage, float extraTargetDamage, bool stunOnCollision)
    {
        enemy?.ApplyKnockBack(-direction, impulse, 0.2f);

        if (collisionDamage > 0f)
            TakeDamage(collisionDamage);

        if (stunOnCollision)
            ApplyStun(0.5f);
    }

    private IEnumerator SlowRoutine(float multiplier, float duration)
    {
        CaptureMoveSpeedIfNeeded();

        if (enemy != null && enemy.stats != null)
            enemy.stats.moveSpeed = _baseMoveSpeed * multiplier;

        yield return new WaitForSeconds(Mathf.Max(0f, duration));

        RestoreMoveSpeed();
        _slowCoroutine = null;
    }

    private IEnumerator PoisonRoutine(float damagePerTick, float duration, float tickInterval)
    {
        float elapsed = 0f;
        float safeTickInterval = Mathf.Max(0.01f, tickInterval);
        WaitForSeconds wait = new WaitForSeconds(safeTickInterval);

        while (elapsed < duration && !IsDead)
        {
            yield return wait;
            elapsed += safeTickInterval;
            TakeDamage(damagePerTick);
        }

        _poisonCoroutine = null;
    }

    private void Die()
    {
        IsDead = true;

        if (targetRigidbody != null)
            targetRigidbody.linearVelocity = Vector2.zero;

        if (enemy != null)
        {
            NotifyEnemyDead();

            if (enemy.DieState != null)
                enemy.ChangeState(enemy.DieState);
            else
                Destroy(enemy.gameObject, 1f);
        }

        enabled = false;
    }

    private void NotifyEnemyDead()
    {
        if (_notifiedDeath || enemy == null)
            return;

        _notifiedDeath = true;

        if (EnemyOnDeadField?.GetValue(enemy) is Action<Enemy> onDead)
            onDead.Invoke(enemy);
    }

    private void HandleEnemyDead(Enemy deadEnemy)
    {
        if (deadEnemy == enemy)
        {
            IsDead = true;
            _notifiedDeath = true;
        }
    }

    private void BindReferences()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (targetRigidbody == null)
            targetRigidbody = GetComponent<Rigidbody2D>();
    }

    private void InitializeHitPointsIfNeeded()
    {
        if (_currentHp > 0f)
            return;

        InitializeHitPointsFromCurrentStats();
    }

    private void InitializeHitPointsFromCurrentStats()
    {
        _currentHp = enemy != null && enemy.stats != null
            ? Mathf.Max(1f, enemy.stats.maxHealth)
            : 1f;
        IsDead = false;
    }

    private void CaptureMoveSpeedIfNeeded()
    {
        if (_hasMoveSpeedSnapshot || enemy == null || enemy.stats == null)
            return;

        _baseMoveSpeed = enemy.stats.moveSpeed;
        _hasMoveSpeedSnapshot = true;
    }

    private void RestoreMoveSpeed()
    {
        if (!_hasMoveSpeedSnapshot || enemy == null || enemy.stats == null)
            return;

        enemy.stats.moveSpeed = _baseMoveSpeed;
    }
}
