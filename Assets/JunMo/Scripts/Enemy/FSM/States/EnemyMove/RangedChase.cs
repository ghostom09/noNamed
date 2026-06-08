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
        float chaseRange = _enemy.stats.chaseRange;

        if (distance < kiting)
        {
            _enemy.rb.linearVelocity = -dir * _enemy.stats.moveSpeed;
        }
        else if (distance >= kiting && distance < chaseRange)
        {
            _enemy.rb.linearVelocity = Vector2.zero;
        }
    }
}