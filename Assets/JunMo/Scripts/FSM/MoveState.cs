using UnityEngine;

public class MoveState : IState
{
    private Enemy _enemy;

    public MoveState(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        
    }

    public void Update()
    {
        _enemy.Movement.Move();
    }

    public void Exit()
    {
        
    }
}