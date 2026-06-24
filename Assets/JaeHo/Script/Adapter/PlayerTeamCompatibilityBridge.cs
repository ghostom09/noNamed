using System.Reflection;
using BossSystem.Boss.FireBoss;
using BossSystem.Boss.WaterBoss;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerTeamCompatibilityBridge : MonoBehaviour, ISlowable, IBindable, IKnockbackable
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Rigidbody2D targetRigidbody;
    [SerializeField] private PlayerMove playerMove;

    private BossSystem.Boss.FireBoss.PlayerHealth _fireBossHealth;
    private PlayerMovement _waterBossMovement;
    private FieldInfo _fireCurrentHpField;
    private FieldInfo _fireMaxHpField;
    private float _lastFireProxyHp;

    private float _slowMultiplier = 1f;
    private float _slowEndTime;
    private float _bindEndTime;
    private bool _appliedMovementEffect;

    private bool IsBound => Time.time < _bindEndTime;
    private float MovementMultiplier => IsBound ? 0f : _slowMultiplier;

    private void Awake()
    {
        BindReferences();
        EnsureTeamProxyComponents();
    }

    private void OnEnable()
    {
        BindReferences();
        EnsureTeamProxyComponents();
    }

    private void LateUpdate()
    {
        SyncFireBossProxyDamage();
        SyncWaterBossMovementEffects();
        ApplyMovementEffects();
    }

    public void ApplySlow(float multiplier, float duration)
    {
        _slowMultiplier = Mathf.Min(_slowMultiplier, Mathf.Clamp01(multiplier));
        _slowEndTime = Mathf.Max(_slowEndTime, Time.time + Mathf.Max(0f, duration));
    }

    public void ApplyBind(float duration, float damagePerTick, float tickInterval)
    {
        _bindEndTime = Mathf.Max(_bindEndTime, Time.time + Mathf.Max(0f, duration));

        if (damagePerTick > 0f)
            playerHealth?.TakeDamage(damagePerTick);
    }

    public void ApplyKnockback(Vector2 direction, float impulse, float collisionDamage, float extraTargetDamage, bool stunOnCollision)
    {
        if (targetRigidbody != null && impulse > 0f)
            targetRigidbody.AddForce(direction.normalized * impulse, ForceMode2D.Impulse);

        if (collisionDamage > 0f)
            playerHealth?.TakeDamage(collisionDamage);

        if (extraTargetDamage > 0f)
            playerHealth?.TakeDamage(extraTargetDamage);

        if (stunOnCollision)
            _bindEndTime = Mathf.Max(_bindEndTime, Time.time + 0.25f);
    }

    private void BindReferences()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (targetRigidbody == null)
            targetRigidbody = GetComponent<Rigidbody2D>();

        if (playerMove == null)
            playerMove = GetComponent<PlayerMove>();
    }

    private void EnsureTeamProxyComponents()
    {
        if (_fireBossHealth == null)
            _fireBossHealth = GetComponent<BossSystem.Boss.FireBoss.PlayerHealth>() ??
                              gameObject.AddComponent<BossSystem.Boss.FireBoss.PlayerHealth>();

        if (_waterBossMovement == null)
            _waterBossMovement = GetComponent<PlayerMovement>() ?? gameObject.AddComponent<PlayerMovement>();

        _fireCurrentHpField ??= typeof(BossSystem.Boss.FireBoss.PlayerHealth)
            .GetField("currentHP", BindingFlags.Instance | BindingFlags.NonPublic);
        _fireMaxHpField ??= typeof(BossSystem.Boss.FireBoss.PlayerHealth)
            .GetField("maxHP", BindingFlags.Instance | BindingFlags.NonPublic);

        ResetFireProxyHp();
    }

    private void SyncFireBossProxyDamage()
    {
        if (_fireBossHealth == null || _fireCurrentHpField == null) return;

        float currentProxyHp = (float)_fireCurrentHpField.GetValue(_fireBossHealth);
        float damage = Mathf.Max(0f, _lastFireProxyHp - currentProxyHp);

        if (damage > 0f)
            playerHealth?.TakeDamage(damage);

        ResetFireProxyHp();
    }

    private void ResetFireProxyHp()
    {
        if (_fireBossHealth == null || _fireCurrentHpField == null || _fireMaxHpField == null) return;

        float proxyMaxHp = playerHealth != null && playerHealth.MaxHp > 0f
            ? playerHealth.MaxHp
            : (float)_fireMaxHpField.GetValue(_fireBossHealth);

        _fireMaxHpField.SetValue(_fireBossHealth, proxyMaxHp);
        _fireCurrentHpField.SetValue(_fireBossHealth, proxyMaxHp);
        _lastFireProxyHp = proxyMaxHp;
    }

    private void SyncWaterBossMovementEffects()
    {
        if (_waterBossMovement == null) return;

        if (_waterBossMovement.IsBound)
            _bindEndTime = Mathf.Max(_bindEndTime, Time.time + Time.deltaTime + 0.05f);

        if (_waterBossMovement.SpeedMult < 1f)
        {
            _slowMultiplier = Mathf.Min(_slowMultiplier, Mathf.Clamp01(_waterBossMovement.SpeedMult));
            _slowEndTime = Mathf.Max(_slowEndTime, Time.time + Time.deltaTime + 0.05f);
        }
    }

    private void ApplyMovementEffects()
    {
        bool hasActiveSlow = Time.time < _slowEndTime;
        bool hasActiveBind = IsBound;

        if (!hasActiveSlow)
            _slowMultiplier = 1f;

        if (hasActiveSlow || hasActiveBind)
        {
            playerMove?.SetExternalMoveSpeedMultiplier(MovementMultiplier);
            _appliedMovementEffect = true;
            return;
        }

        if (_appliedMovementEffect)
        {
            playerMove?.SetExternalMoveSpeedMultiplier(1f);
            _appliedMovementEffect = false;
        }
    }
}

public static class BossDamageUtility
{
    public static bool TryDamagePlayer(Collider2D target, float damage)
    {
        if (target == null || !target.CompareTag("Player"))
            return false;

        if (!CombatComponentUtility.TryGet(target, out IDamageable damageable))
            return false;

        damageable.TakeDamage(Mathf.Max(0f, damage));
        return true;
    }

    public static bool TryDamagePlayer(GameObject target, float damage)
    {
        if (target == null || !target.CompareTag("Player"))
            return false;

        if (!TryGetDamageable(target, out IDamageable damageable))
            return false;

        damageable.TakeDamage(Mathf.Max(0f, damage));
        return true;
    }

    private static bool TryGetDamageable(GameObject target, out IDamageable damageable)
    {
        if (target.CompareTag("Player") &&
            target.GetComponentInParent<PlayerHealth>() is PlayerHealth playerHealth)
        {
            damageable = playerHealth;
            return true;
        }

        if (target.TryGetComponent(out damageable))
            return true;

        damageable = target.GetComponentInParent<IDamageable>();
        return damageable != null;
    }
}
