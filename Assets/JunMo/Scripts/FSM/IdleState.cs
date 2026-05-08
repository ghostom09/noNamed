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
        Debug.Log("Enter");
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
            _enemy.Movement.Move();
        }
    }

    public void Exit()
    {
        Debug.Log("Exit");
    }
}
