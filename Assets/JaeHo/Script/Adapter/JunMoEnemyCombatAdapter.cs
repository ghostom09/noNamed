using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class JunMoEnemyCombatAdapter : MonoBehaviour, IDamageable, IHitPointStatus, ISlowable, IPoisonable, IStunnable, IBindable, IKnockbackable
{
    private const string EnemyLayerName = "Enemy";

    [SerializeField] private Enemy enemy;
    [SerializeField] private Rigidbody2D targetRigidbody;
    [SerializeField] private bool syncColliderLayersToEnemyLayer = true;

    private Coroutine _poisonCoroutine;
    private Coroutine _slowCoroutine;
    private float _currentHp;
    private float _baseMoveSpeed;
    private bool _hasMoveSpeedSnapshot;
    private bool _hasTakenDamage;
    private bool _isDead;
    private bool _notifiedDeath;

    private static readonly FieldInfo EnemyOnDeadField = typeof(Enemy).GetField(
        "OnDead",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public float CurrentHp => _currentHp;
    public bool IsDead => _isDead;

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

        float damage = Mathf.Max(0f, amount);
        float hpBefore = _currentHp;
        bool damagedJunMoEnemy = TryDamageJunMoEnemy(damage);
        _currentHp = damagedJunMoEnemy
            ? Mathf.Max(0f, enemy.CurrentHealth)
            : Mathf.Max(0f, _currentHp - damage);

        Debug.Log($"[Enemy Hit] {gameObject.name} damage:{amount:0.##} hp:{hpBefore:0.##}->{_currentHp:0.##}");

        if (_currentHp <= 0f)
        {
            Die(!damagedJunMoEnemy);
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

    private void Die(bool changeEnemyState)
    {
        _isDead = true;

        if (targetRigidbody != null)
            targetRigidbody.linearVelocity = Vector2.zero;

        if (enemy != null)
        {
            NotifyEnemyDead();

            if (changeEnemyState)
            {
                if (enemy.DieState != null)
                    enemy.ChangeState(enemy.DieState);
                else
                    Destroy(enemy.gameObject, 1f);
            }
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
            _isDead = true;
            _notifiedDeath = true;
        }
    }

    private void BindReferences()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (targetRigidbody == null)
            targetRigidbody = GetComponent<Rigidbody2D>();

        ConfigureHitDetectionLayer();
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
        _isDead = false;
    }

    private bool TryDamageJunMoEnemy(float damage)
    {
        if (enemy == null || damage <= 0f || enemy.CurrentHealth <= 0f)
            return false;

        enemy.TakeDamage(damage);
        return true;
    }

    private void ConfigureHitDetectionLayer()
    {
        if (!syncColliderLayersToEnemyLayer)
            return;

        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        if (enemyLayer < 0)
            return;

        gameObject.layer = enemyLayer;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null)
                continue;

            collider.gameObject.layer = enemyLayer;
        }
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
