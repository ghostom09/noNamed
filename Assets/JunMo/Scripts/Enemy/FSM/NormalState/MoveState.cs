using UnityEngine;

public class MoveState : IState
{
    private Enemy _enemy;
    
    private Vector3 _currentDir;
    private float _dirChangeTimer;

    public MoveState(Enemy enemy) // 감시
    {
        _enemy = enemy;
        
        DecideDirection();
    }

    public void Enter()
    {
        
    }

    public void Update()
    {
        if (_enemy.CanChaseRange())
        {
            _enemy.ChangeState(_enemy.ChaseState);
            return;
        }
        
        _dirChangeTimer -= Time.deltaTime;

        if (_dirChangeTimer <= 0f)
        {
            DecideDirection();
        }

        _enemy.rb.linearVelocity =
            _currentDir * _enemy.stats.moveSpeed;
    }
    
    private void DecideDirection()
    {
        Vector2 random2D =
            Random.insideUnitCircle.normalized;

        Vector3 randomDir =
            new Vector3(random2D.x, random2D.y, 0);

        Vector3 playerDir =
            _enemy.GetVector2();

        float playerBias =
            Random.Range(0.1f, 0.5f);

        _currentDir =
            Vector3.Lerp(randomDir, playerDir, playerBias)
                .normalized;

        _dirChangeTimer =
            Random.Range(0.5f, 1f);
    }

    public void Exit()
    {
        
    }
}