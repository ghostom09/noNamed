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
        _enemy.Attack.Attack();
    }

    public void Update()
    {
        if (_enemy.CanAttackRange() && _enemy.CanAttackSpeed())
        {
            _enemy.Attack.Attack();
            return;
        }
        if (_enemy.CanChaseRange())
        {
            _enemy.ChangeState(_enemy.ChaseState); // 상태 전환
            return;
        }
        _enemy.ChangeState(_enemy.IdleState);
    }

    public void Exit()
    {
        _enemy.ResetAttackTimer();
    }
}