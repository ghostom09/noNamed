using UnityEngine;

namespace BossSystem.Boss.FireBoss
{
    public class FireballProjectile : MonoBehaviour
    {
        private Vector3 direction;
        private float speed;
        private bool isGasTrigger;
        private FireBossController boss;
        private float damage = 25f;
        private float lifetime = 5f;

        public void Initialize(Vector3 dir, float spd, bool gasTrigger,
                               FireBossController bossRef)
        {
            direction = new Vector3(dir.x, dir.y, 0f).normalized;
            speed = spd;
            isGasTrigger = gasTrigger;
            boss = bossRef;

            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider == null)
                circleCollider = gameObject.AddComponent<CircleCollider2D>();

            circleCollider.radius = 0.5f;
            circleCollider.isTrigger = true;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody2D>();

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            GasCloud gas = other.GetComponent<GasCloud>();
            if (gas != null)
            {
                if (!gas.IsSpread)
                    return;

                if (isGasTrigger && boss != null)
                    boss.TriggerGasExplosion(transform.position, boss.GetFireballSize() * 0.5f);

                Destroy(gameObject);
                return;
            }

            if (other.isTrigger)
                return;

            if (other.CompareTag("Player"))
                FireBossController.ApplyDamageToPlayer(other.gameObject, damage);

            if (isGasTrigger && boss != null)
                boss.TriggerGasExplosion(transform.position, boss.GetFireballSize() * 0.5f);

            Destroy(gameObject);
        }
    }
}
