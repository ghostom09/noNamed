using UnityEngine;

public class MeleeMovement : IMovement
{
    private Enemy _enemy;

    public MeleeMovement(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Move()
    {
        float speed = _enemy.stats.moveSpeed;
        _enemy.rb.linearVelocity = new Vector2(Random.Range(-speed, speed), Random.Range(-speed, speed));
    }
}