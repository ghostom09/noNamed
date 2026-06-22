using UnityEngine;

namespace BossSystem.Boss.WaterBoss
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class FloodZone : MonoBehaviour
    {
        [SerializeField] private float slowMultiplier = 0.5f;
        [SerializeField] private float tickInterval   = 0.25f;
        private float lastTick = 0f;

        public void Initialize(Vector2 size)
        {
            transform.position = Vector3.zero;
            transform.localScale = Vector3.one;

            var col = GetComponent<BoxCollider2D>();
            if (col == null)
                col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;

            var sprite = GetComponent<SpriteRenderer>();
            if (sprite != null)
                sprite.size = size;
        }

        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null)
                col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (Time.time - lastTick < tickInterval) return;
            lastTick = Time.time;
            other.GetComponent<PlayerMovement>()?.ApplySlow(slowMultiplier, tickInterval + 0.1f);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                other.GetComponent<PlayerMovement>()?.RemoveSlow();
        }
    }
}
