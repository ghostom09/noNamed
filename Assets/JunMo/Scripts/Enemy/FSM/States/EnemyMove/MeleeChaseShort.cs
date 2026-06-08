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
        Debug.Log("<color=red>Fatal Error:</color> 9150");
        Vector2 toPlayer = _enemy.GetVector2();
        _enemy.rb.linearVelocity = toPlayer * _enemy.stats.moveSpeed;
    }
}