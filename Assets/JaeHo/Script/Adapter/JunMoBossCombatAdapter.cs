using System.Collections;
using BossSystem.Boss;
using UnityEngine;

[DisallowMultipleComponent]
public class JunMoBossCombatAdapter : MonoBehaviour, IDamageable, IHitPointStatus, ISlowable, IPoisonable, IStunnable, IBindable, IKnockbackable
{
    [SerializeField] private BossBase boss;
    [SerializeField] private Rigidbody2D targetRigidbody;

    private Coroutine _poisonCoroutine;
    private Coroutine _slowCoroutine;
    private float _speedMultiplier = 1f;

    public float CurrentHp => boss != null ? boss.CurrentHP : 0f;
    public bool IsDead => boss == null || boss.IsDead;

    private void Awake()
    {
        if (boss == null)
            boss = GetComponent<BossBase>();

        if (targetRigidbody == null)
            targetRigidbody = GetComponent<Rigidbody2D>();

        ConfigureBossRigidbody();
    }

    private void OnEnable()
    {
        ConfigureBossRigidbody();
    }

    private void LateUpdate()
    {
        if (targetRigidbody == null || Mathf.Approximately(_speedMultiplier, 1f))
            return;

        targetRigidbody.linearVelocity *= _speedMultiplier;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || boss == null)
            return;

        float hpBefore = boss.CurrentHP;
        boss.TakeDamage(Mathf.Max(0f, amount));
        SyncBossHpUi();

        Debug.Log(
            $"[Boss Hit] {gameObject.name} damage:{amount:0.##} hp:{hpBefore:0.##}->{boss.CurrentHP:0.##}");
    }

    private void SyncBossHpUi()
    {
        if (boss == null)
            return;

        BossHpBarUI[] bossHpBars = FindObjectsByType<BossHpBarUI>(FindObjectsInactive.Include);
        for (int i = 0; i < bossHpBars.Length; i++)
        {
            if (bossHpBars[i] == null)
                continue;

            bossHpBars[i].SetBoss(boss);
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
        if (targetRigidbody != null)
            targetRigidbody.linearVelocity = Vector2.zero;
    }

    public void ApplyBind(float duration, float damagePerTick, float tickInterval)
    {
        ApplyStun(duration);

        if (damagePerTick > 0f && tickInterval > 0f)
            ApplyPoison(damagePerTick, duration, tickInterval);
    }

    public void ApplyKnockback(Vector2 direction, float impulse, float collisionDamage, float extraTargetDamage, bool stunOnCollision)
    {
        if (collisionDamage > 0f)
            TakeDamage(collisionDamage);
    }

    private void ConfigureBossRigidbody()
    {
        if (targetRigidbody == null)
            targetRigidbody = GetComponent<Rigidbody2D>();

        if (targetRigidbody == null)
            return;

        targetRigidbody.mass = 10000f;
        targetRigidbody.freezeRotation = true;
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
}
