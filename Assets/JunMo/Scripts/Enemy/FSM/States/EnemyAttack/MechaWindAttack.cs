using UnityEngine;
using System.Collections;

public class MechaWindAttack : IAttack
{
    private readonly Enemy _enemy;
    private readonly MonoBehaviour _coroutineRunner;
 
    private const float SwingAngle = 120f;  // 부채꼴 각도
    private const float SwingRange = 2f;    // 공격 범위
    private const int RayCount = 10;        // 부채꼴 판정 정밀도
 
    public MechaWindAttack(Enemy enemy)
    {
        _enemy = enemy;
        _coroutineRunner = enemy;
    }
 
    public void Attack()
    {
        if (!_enemy.CanAttackRange() || !_enemy.CanAttackSpeed()) return;
 
        _enemy.ResetAttackTimer();
        _coroutineRunner.StartCoroutine(SwingAttack());
    }
 
    private IEnumerator SwingAttack()
    {
        // 모션 딜레이 (애니메이션 연동 가능)
        yield return new WaitForSeconds(0.2f);
 
        Vector2 baseDir = _enemy.GetVector2();
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
 
        // 부채꼴 범위 내 플레이어 감지
        for (int i = 0; i < RayCount; i++)
        {
            float angle = baseAngle - SwingAngle / 2f + (SwingAngle / RayCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
 
            RaycastHit2D hit = Physics2D.Raycast(_enemy.transform.position, dir, SwingRange);
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                // hit.collider.GetComponent<PlayerStats>()?.TakeDamage(_enemy.stats.damage);
                Debug.Log("[Meka2] 부채꼴 공격 히트");
                break; // 한 번만 데미지
            }
        }
    }
}