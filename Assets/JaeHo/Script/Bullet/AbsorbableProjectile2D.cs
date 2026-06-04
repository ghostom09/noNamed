using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class AbsorbableProjectile2D : MonoBehaviour, IAbsorbableProjectile
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask reflectedTargetLayer;
    [SerializeField] private bool destroyOnHit = true;

    private Rigidbody2D _rb;
    private GameObject _owner;
    private bool _reflected;
    private float _reflectedDamage;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Absorb(GameObject absorber)
    {
        gameObject.SetActive(false);
    }

    public void Reflect(GameObject reflector, Vector2 direction, float damageMultiplier)
    {
        _owner = reflector;
        _reflected = true;
        _reflectedDamage = damage * Mathf.Max(0f, damageMultiplier);

        Vector2 reflectedDirection = direction.sqrMagnitude > 0f ? direction.normalized : -_rb.linearVelocity.normalized;
        float speed = Mathf.Max(_rb.linearVelocity.magnitude, 8f);
        _rb.linearVelocity = reflectedDirection * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyReflectedHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryApplyReflectedHit(collision.collider);
    }

    private void TryApplyReflectedHit(Collider2D other)
    {
        if (!_reflected) return;
        if (_owner != null && other.transform.root.gameObject == _owner) return;
        if ((reflectedTargetLayer.value & (1 << other.gameObject.layer)) == 0) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(_reflectedDamage);

            if (destroyOnHit)
                gameObject.SetActive(false);
        }
    }
}
