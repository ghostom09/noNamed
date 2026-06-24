using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable, IHitPointStatus
{
    [Header("--- Health Settings ---")]
    [SerializeField] private float maxHp = 100f;

    public event Action<float, float> OnDamaged;
    public event Action<float, float> OnHealed;
    public event Action OnDeath;
    
    public float CurrentHp { get; private set; }
    public float MaxHp => maxHp;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHp = maxHp;
    }

    public void SetMaxHp(float value, bool fillHp = true)
    {
        maxHp = Mathf.Max(1f, value);

        if (fillHp)
        {
            CurrentHp = maxHp;
        }
        else
        {
            CurrentHp = Mathf.Min(CurrentHp, maxHp);
        }

        OnHealed?.Invoke(CurrentHp, maxHp);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        amount = Mathf.Max(0f, amount);
        if (amount <= 0f) return;
        
        CurrentHp = Mathf.Max(CurrentHp - amount, 0);
        OnDamaged?.Invoke(CurrentHp, maxHp);

        if (CurrentHp <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if(IsDead) return;

        CurrentHp = Mathf.Min(CurrentHp + amount, maxHp);
        OnHealed?.Invoke(CurrentHp, maxHp);
    }

    private void Die()
    {
        IsDead = true;
        OnDeath?.Invoke();
    }
}
