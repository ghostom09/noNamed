using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KnockbackHandler : MonoBehaviour, IKnockbackable
{
    [Header("--- Collision Bonus ---")]
    [SerializeField] private LayerMask collisionDamageLayer;
    [SerializeField] private float collisionWindow = 0.35f;
    [SerializeField] private float collisionStunDuration = 0.5f;

    private Rigidbody2D _rb;
    private float _collisionDamage;
    private float _extraTargetDamage;
    private bool _stunOnCollision;
    private float _collisionWindowEndsAt;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyKnockback(Vector2 direction, float impulse, float collisionDamage, float extraTargetDamage, bool stunOnCollision)
    {
        if (impulse <= 0f) return;

        Vector2 normalizedDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;

        _collisionDamage = Mathf.Max(_collisionDamage, collisionDamage);
        _extraTargetDamage = Mathf.Max(_extraTargetDamage, extraTargetDamage);
        _stunOnCollision |= stunOnCollision;
        _collisionWindowEndsAt = Time.time + collisionWindow;

        _rb.AddForce(normalizedDirection * impulse, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time > _collisionWindowEndsAt) return;
        if ((collisionDamageLayer.value & (1 << collision.collider.gameObject.layer)) == 0) return;

        if (_collisionDamage > 0f && TryGetComponent<IDamageable>(out var selfDamageable))
            selfDamageable.TakeDamage(_collisionDamage);

        if (_extraTargetDamage > 0f && collision.collider.TryGetComponent<IDamageable>(out var otherDamageable))
            otherDamageable.TakeDamage(_extraTargetDamage);

        if (_stunOnCollision && TryGetComponent<IStunnable>(out var stunnable))
            stunnable.ApplyStun(collisionStunDuration);

        _collisionWindowEndsAt = 0f;
        _collisionDamage = 0f;
        _extraTargetDamage = 0f;
        _stunOnCollision = false;
    }
}
