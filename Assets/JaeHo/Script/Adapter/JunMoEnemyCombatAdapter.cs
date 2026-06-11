using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class JunMoEnemyCombatAdapter : MonoBehaviour, IDamageable, IHitPointStatus, ISlowable, IPoisonable, IStunnable, IBindable, IKnockbackable
{
    [SerializeField] private Enemy enemy;
    [SerializeField] private Rigidbody2D targetRigidbody;

    private Coroutine _poisonCoroutine;
    private Coroutine _slowCoroutine;
    private float _currentHp;
    private float _speedMultiplier = 1f;

    public float CurrentHp => _currentHp;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (targetRigidbody == null)
            targetRigidbody = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _currentHp = enemy != null && enemy.stats != null ? enemy.stats.maxHealth : 1f;
        IsDead = false;
    }

    private void OnEnable()
    {
        if (enemy != null)
            enemy.OnDead += HandleEnemyDead;
    }

    private void OnDisable()
    {
        if (enemy != null)
            enemy.OnDead -= HandleEnemyDead;
    }

    private void LateUpdate()
    {
        if (targetRigidbody == null || Mathf.Approximately(_speedMultiplier, 1f)) return;

        targetRigidbody.linearVelocity *= _speedMultiplier;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        float hpBefore = _currentHp;
        _currentHp = Mathf.Max(0f, _currentHp - Mathf.Max(0f, amount));

        Debug.Log(
            $"[Enemy Hit] {gameObject.name} damage:{amount:0.##} hp:{hpBefore:0.##}->{_currentHp:0.##}");

        if (_currentHp <= 0f)
        {
            Debug.Log($"[Enemy Dead] {gameObject.name}");
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
        _speedMultiplier = multiplier;
        yield return new WaitForSeconds(Mathf.Max(0f, duration));
        _speedMultiplier = 1f;
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

        if (enemy != null && enemy.DieState != null)
            enemy.ChangeState(enemy.DieState);

        enabled = false;
    }

    private void HandleEnemyDead(Enemy deadEnemy)
    {
        if (deadEnemy == enemy)
            IsDead = true;
    }
}
