using UnityEngine;

public class BulletBase : MonoBehaviour
{
    protected float damage;
    protected Vector2 direction;
    protected float speed = 10f;
    protected float lifeTime = 3f;

    public virtual void Init(float damage, Vector2 direction, float speed = 10f)
    {
        this.damage = damage;
        this.direction = direction;
        this.speed = speed;
        Destroy(gameObject, lifeTime);
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
        // if (other.gameObject.TryGetComponent<IDamageable>(out var damageable))
        // {
        //     damageable.TakeDamage(damage);
        //     Debug.Log($"데미지 적용 {damage}");
        // }
    }
}