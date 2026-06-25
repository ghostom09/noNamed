using UnityEngine;

public class BulletBase : MonoBehaviour
{
    [SerializeField] private float spriteAngleOffset = -90f;

    protected float damage;
    protected Vector2 direction;
    protected float speed = 10f;
    protected float lifeTime = 3f;

    public virtual void Init(float damage, Vector2 direction, float speed = 10f)
    {
        this.damage = damage;
        this.speed = speed;
        SetDirection(direction);
        Destroy(gameObject, lifeTime);
    }

    protected void SetDirection(Vector2 newDirection)
    {
        if (newDirection.sqrMagnitude <= Mathf.Epsilon)
            return;

        direction = newDirection.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);
    }

    protected virtual void Update()
    {
        transform.Translate(speed * Time.deltaTime * direction, Space.World);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        OnHitPlayer(other);
        Destroy(gameObject);
    }

    protected virtual void OnHitPlayer(Collider2D other)
    {
        if (other.gameObject.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
            Debug.Log($"데미지 적용 {damage}");
        }
    }
}
