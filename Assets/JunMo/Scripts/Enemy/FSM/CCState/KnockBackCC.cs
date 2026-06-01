using UnityEngine;

public class KnockBackCC : ICC
{
    private Enemy _enemy;
    private float _duration;
    private float _timer;
    public bool IsDone => _timer >= _duration;

    public KnockBackCC(Enemy enemy) { _enemy = enemy; }

    public void Apply(Vector2 hitDir, float force, float duration)
    {
        _duration = duration;
        _timer = 0f;
        _enemy.rb.linearVelocity = -hitDir.normalized * force;
    }

    public void Apply() { }

    public void Update() => _timer += Time.deltaTime;

    public void Exit()
    {
        _enemy.rb.linearVelocity = Vector2.zero;
    }
}