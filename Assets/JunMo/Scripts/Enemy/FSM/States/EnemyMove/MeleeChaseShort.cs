using UnityEngine;

public class MeleeChaseShort : IChase
{
    private Enemy _enemy;

    public MeleeChaseShort(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Chase()
    {
        Vector2 toPlayer = _enemy.GetVector2();
        
        Vector2 toPlayerRaw = _enemy.GetVectorNotNormalized();
        float distance = toPlayerRaw.magnitude;

        if (distance <= 1f)
        {
            _enemy.rb.linearVelocity = Vector2.zero;
            return;
        }
        _enemy.rb.linearVelocity = toPlayer * _enemy.stats.moveSpeed;
    }
}