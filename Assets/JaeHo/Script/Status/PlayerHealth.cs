using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class PlayerHealth : MonoBehaviour, IDamageable, IHitPointStatus
{
    [SerializeField] private Health health;
    [SerializeField] private PlayerStatManager statManager;
    [SerializeField, Min(0f)] private float invincibleTimeAfterHit = 0.6f;
    [SerializeField] private bool logHitDebug = true;

    public event Action<float, float> OnDamaged;
    public event Action<float, float> OnHealed;
    public event Action OnDeath;

    public float CurrentHp => health != null ? health.CurrentHp : 0f;
    public float MaxHp => health != null ? health.MaxHp : 0f;
    public bool IsDead => health != null && health.IsDead;
    public bool IsInvincible => Time.time < invincibleUntil;

    private float invincibleUntil;

    private void Awake()
    {
        BindReferences();
        ApplyStatMaxHp(true);
    }

    private void Start()
    {
        ApplyStatMaxHp(true);
    }

    private void OnEnable()
    {
        BindReferences();

        if (health != null)
        {
            health.OnDamaged += HandleDamaged;
            health.OnHealed += HandleHealed;
            health.OnDeath += HandleDeath;
        }

        if (statManager != null)
        {
            statManager.OnStatsChanged += HandleStatsChanged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnHealed -= HandleHealed;
            health.OnDeath -= HandleDeath;
        }

        if (statManager != null)
        {
            statManager.OnStatsChanged -= HandleStatsChanged;
        }
    }

    public void TakeDamage(float amount)
    {
        if (health == null || health.IsDead) return;

        if (IsInvincible)
        {
            if (logHitDebug)
                Debug.Log($"[Player Hit] Invincible - ignored damage:{amount:0.##}", this);

            return;
        }

        float hpBefore = health.CurrentHp;

        health.TakeDamage(amount);
        invincibleUntil = Time.time + invincibleTimeAfterHit;

        if (logHitDebug)
        {
            Debug.Log(
                $"[Player Hit] damage:{amount:0.##} hp:{hpBefore:0.##}->{health.CurrentHp:0.##}/{health.MaxHp:0.##}",
                this);
        }
    }

    public void Heal(float amount)
    {
        if (health == null) return;

        health.Heal(amount);
    }

    public void ResetInvincibleTime()
    {
        invincibleUntil = 0f;
    }

    private void HandleStatsChanged()
    {
        ApplyStatMaxHp(false);
    }

    private void ApplyStatMaxHp(bool fillHp)
    {
        if (health == null || statManager == null) return;
        if (!statManager.RefreshStats()) return;

        health.SetMaxHp(statManager.Health, fillHp);
    }

    private void BindReferences()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (statManager == null)
            statManager = GetComponent<PlayerStatManager>();
    }

    private void HandleDamaged(float currentHp, float maxHp)
    {
        OnDamaged?.Invoke(currentHp, maxHp);
    }

    private void HandleHealed(float currentHp, float maxHp)
    {
        OnHealed?.Invoke(currentHp, maxHp);
    }

    private void HandleDeath()
    {
        OnDeath?.Invoke();
    }
}
