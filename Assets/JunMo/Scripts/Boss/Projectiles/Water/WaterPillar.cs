using System.Collections;
using BossSystem.Boss.FireBoss;
using UnityEngine;
namespace BossSystem.Boss.WaterBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  물기둥
    //  지정 위치에 낙하 → 피해 + 속박 적용 → bindDuration 유지 후 소멸
    // ═══════════════════════════════════════════════════════════════
    public class WaterPillar : MonoBehaviour
    {
        private float radius;
        private float damage;
        private float bindDuration;

        public void Initialize(float r, float dmg, float bindDur)
        {
            radius       = r;
            damage       = dmg;
            bindDuration = bindDur;
            StartCoroutine(PillarLifecycle());
        }

        private IEnumerator PillarLifecycle()
        {
            // 낙하 연출: 위에서 아래로 스케일 확장
            float dropTime = 0.2f;
            float elapsed  = 0f;
            transform.localScale = new Vector3(1f, 0f, 1f);

            while (elapsed < dropTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dropTime;
                transform.localScale = new Vector3(1f, Mathf.Lerp(0f, 1f, t), 1f);
                yield return null;
            }
            transform.localScale = Vector3.one;

            // 피해 + 속박 판정
            var hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;
                hit.GetComponent<PlayerHealth>()?.TakeDamage(damage);
                hit.GetComponent<PlayerMovement>()?.ApplyBind(bindDuration);
            }

            // bindDuration 유지
            yield return new WaitForSeconds(bindDuration);

            // 페이드아웃
            float fadeTime = 0.35f;
            elapsed = 0f;
            var renderers = GetComponentsInChildren<Renderer>();
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeTime);
                foreach (var rend in renderers)
                {
                    var c = rend.material.color;
                    rend.material.color = new Color(c.r, c.g, c.b, alpha);
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}