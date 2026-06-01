using UnityEngine;

public class CCState : IState
{
    private Enemy _enemy;
    private ICC _currentCC;

    public CCState(Enemy enemy) { _enemy = enemy; }

    public void ApplyStun(float duration)
    {
        var cc = new StunCC(_enemy);
        cc.Apply(duration);
        _currentCC = cc;
        _enemy.ChangeState(this);
    }

    public void ApplySnare(float duration)
    {
        var cc = new SnareCC(_enemy);
        cc.Apply(duration);
        _currentCC = cc;
        _enemy.ChangeState(this);
    }

    public void ApplyKnockback(Vector2 hitDir, float force, float duration)
    {
        var cc = new KnockBackCC(_enemy);
        cc.Apply(hitDir, force, duration);
        _currentCC = cc;
        _enemy.ChangeState(this);
    }

    public void Enter() { _enemy.rb.linearVelocity = Vector2.zero; }

    public void Update()
    {
        if (_currentCC == null) return;
        _currentCC.Update();
        if (_currentCC.IsDone)
        {
            _currentCC.Exit();
            _enemy.ChangeState(_enemy.IdleState);
        }
    }

    public void Exit() { }
}