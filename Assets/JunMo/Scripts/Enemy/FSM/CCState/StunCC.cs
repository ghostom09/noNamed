using UnityEngine;

public class StunCC : ICC
{
    private Enemy _enemy;
    private float _duration;
    private float _timer;
    public bool IsDone => _timer >= _duration;

    public StunCC(Enemy enemy) { _enemy = enemy; }

    public void Apply(float duration)
    {
        _duration = duration;
        _timer = 0f;
    }

    public void Apply() { }

    public void Update() => _timer += Time.deltaTime;

    public void Exit()
    {
        _enemy.rb.linearVelocity = Vector2.zero;
    }
}