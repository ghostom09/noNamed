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
        _enemy.Chase.Chase();
    }

    public void Exit()
    {
        
    }
}