using UnityEngine;

public class IdleState : IState
{
    private Enemy _enemy;
    
    public IdleState(Enemy enemy)
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
            _enemy.ChangeState(new AttackState(_enemy));
        }
        else if (_enemy.CanChaseRange())
        {
            _enemy.ChangeState(new ChaseState(_enemy));
        }
        else
        {
            _enemy.ChangeState(new MoveState(_enemy));
        }
    }

    public void Exit()
    {
        
    }
}
