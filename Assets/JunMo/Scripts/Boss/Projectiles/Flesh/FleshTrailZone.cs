using UnityEngine;

namespace BossSystem.Boss.FleshBoss
{
    /// <summary>
    /// 패턴 1 돌진 — 지나간 자리에 생성되는 장판
    /// 3초 후 자동 소멸, 닿으면 도트 데미지
    /// </summary>
    public class FleshTrailZone : MonoBehaviour
    {
        [SerializeField] private float damagePerSecond = 3f;
        [SerializeField] private float duration = 3f;
        [SerializeField] private float tickRate = 0.25f;

        private float lastTick;

        private void Start()
        {
            lastTick = Time.time;
            StartCoroutine(FadeOut());
        }

        public void Initialize(float dps, float dur)
        {
            damagePerSecond = dps;
            duration = dur;
            Destroy(gameObject, duration);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time - lastTick < tickRate) return;
            lastTick = Time.time;

            BossDamageUtility.TryDamagePlayer(other, damagePerSecond * tickRate);
        }

        private System.Collections.IEnumerator FadeOut()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer == null) yield break;

            float elapsed = 0f;
            Color startColor = renderer.material.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                renderer.material.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }
    }
}
