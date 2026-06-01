using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [Header("--- Health Settings ---")]
    [SerializeField] private float maxHp = 100f;

    public event Action<float, float> OnDamaged;
    public event Action<float, float> OnHealed;
    public event Action OnDeath;
    
    public float CurrentHp { get; private set; } public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        
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
