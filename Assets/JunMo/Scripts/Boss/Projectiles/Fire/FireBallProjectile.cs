using UnityEngine;

namespace BossSystem.Boss.FireBoss
{
    /// <summary>
    /// 화염탄 투사체
    ///
    /// 수정:
    ///  - 착탄 시 가스 스폰과 폭발을 한 프레임 뒤로 분리
    ///    (방금 스폰한 가스가 같은 프레임에 즉시 폭발하는 문제 수정)
    /// </summary>
    public class FireballProjectile : MonoBehaviour
    {
        private Vector3            direction;
        private float              speed;
        private bool               isGasTrigger;
        private FireBossController boss;
        private float              damage   = 25f;
        private float              lifetime = 5f;

        public void Initialize(Vector3 dir, float spd, bool gasTrigger,
                               FireBossController bossRef)
        {
            direction    = new Vector3(dir.x, dir.y, 0f).normalized;
            speed        = spd;
            isGasTrigger = gasTrigger;
            boss         = bossRef;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.isTrigger) return;

            if (other.CompareTag("Player"))
                other.GetComponent<PlayerHealth>()?.TakeDamage(damage);

            // 착탄 위치 저장 후 오브젝트 비활성, 다음 프레임에 가스 처리
            if (isGasTrigger && boss != null)
                StartCoroutine(HandleGasNextFrame(transform.position));
            else
                Destroy(gameObject);
        }

        private System.Collections.IEnumerator HandleGasNextFrame(Vector3 hitPos)
        {
            // 투사체 렌더러 끄고 이동 정지 (시각적으로 즉시 사라진 것처럼)
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            speed = 0f;

            yield return null; // 한 프레임 대기

            if (boss != null)
            {
                // ① 가스 스폰
                boss.SpawnGasCloud(hitPos, 360f);

                // ② 한 프레임 후에 인접 기존 가스 폭발 체크
                //    방금 스폰한 가스는 아직 activeGasClouds에 등록됐지만
                //    여기선 '기존 가스'만 폭발 대상 (반지름 겹침 체크)
                boss.TriggerGasExplosion(hitPos, 1.5f);
            }

            Destroy(gameObject);
        }
    }
}