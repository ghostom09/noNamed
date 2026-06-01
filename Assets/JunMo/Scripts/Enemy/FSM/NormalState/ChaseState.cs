using UnityEngine;

public class ChaseState : IState
{
    private Enemy _enemy;

    public ChaseState(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        
    }

    public void Update()
    {
        if (_enemy.CanAttackRange() && _enemy.CanAttackSpeed())
        {
            _enemy.ChangeState(_enemy.AttackState);
            return;
        }
        if (_enemy.CanChaseRange())
        {
            _enemy.Chase.Chase();
            return;
        }
        _enemy.ChangeState(_enemy.IdleState);
    }

    public void Exit()
    {
        
    }
}