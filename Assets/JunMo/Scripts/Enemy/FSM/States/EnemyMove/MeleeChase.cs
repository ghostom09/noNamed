using UnityEngine;

public class MeleeChase : IChase
{
    private Enemy _enemy;

    public MeleeChase(Enemy enemy)
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

        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x);

        float zigzagSpeed = 5f;
        float zigzagAngle = 25f * Mathf.Deg2Rad;

        float zigzag = Mathf.Sign(Mathf.Sin(Time.time * zigzagSpeed));

        float finalAngle = baseAngle + zigzag * zigzagAngle;

        Vector2 dir = new Vector2(
            Mathf.Cos(finalAngle),
            Mathf.Sin(finalAngle)
        );

        _enemy.rb.linearVelocity = dir * _enemy.stats.moveSpeed;
    }
}