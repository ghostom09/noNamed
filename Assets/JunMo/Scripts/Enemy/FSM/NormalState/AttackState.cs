using UnityEngine;

public class AttackState : IState
{
    private Enemy _enemy;

    public AttackState(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        _enemy.Animation?.Play(EnemyAnimationType.Idle);
        _enemy.Attack.Attack();
    }

    public void Update()
    {
        _enemy.rb.linearVelocity = Vector2.zero;

        if (_enemy.IsAttacking)
            return;

        if (_enemy.CanAttackRange() && _enemy.CanAttackSpeed())
        {
            _enemy.Animation?.Play(EnemyAnimationType.Idle);
            _enemy.Attack.Attack();
            return;
        }

        if (_enemy.CanChaseRange())
        {
            _enemy.ChangeState(_enemy.ChaseState);
            return;
        }

        _enemy.ChangeState(_enemy.IdleState);
    }

    public void Exit()
    {
        
    }
}
