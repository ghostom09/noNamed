using UnityEngine;

public class ChaseState : IState
{
    private Enemy _enemy;
    private RangedChase _chase;

    public ChaseState(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        _enemy.Animation?.Play(EnemyAnimationType.Walk);
    }

    public void Update()
    {
        if (!_enemy.CanChaseRange())
        {
            _enemy.ChangeState(_enemy.MoveState);
            return;
        }
        _enemy.Chase.Chase();
        if (_enemy.CanAttackRange() && _enemy.CanAttackSpeed())
        {
            _enemy.ChangeState(_enemy.AttackState);
        }
    }

    public void Exit()
    {
        
    }
}
