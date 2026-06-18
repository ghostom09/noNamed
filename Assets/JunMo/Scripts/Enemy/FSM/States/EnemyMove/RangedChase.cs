using UnityEngine;

public class RangedChase : IChase
{
    private Enemy _enemy;
    private bool _isKiting = false;

    public RangedChase(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Chase()
    {
        Vector2 rawDir = (Vector2)_enemy.transform.position - _enemy.GetPlayerVector2();
        float distance = rawDir.magnitude;
        Vector2 dir = _enemy.GetVector2();

        float kiting = _enemy.stats.kitingRange;
        float attackRange = _enemy.stats.attackRange;

        if (distance < kiting)
        {
            // 너무 가까우면 후퇴
            _enemy.rb.linearVelocity = -dir * _enemy.stats.moveSpeed;
        }
        else if (distance <= attackRange)
        {
            // 공격 가능 거리면 정지
            _enemy.rb.linearVelocity = Vector2.zero;
        }
        else
        {
            // 공격 범위 밖이면 접근
            _enemy.rb.linearVelocity = dir * _enemy.stats.moveSpeed;
        }
    }
}