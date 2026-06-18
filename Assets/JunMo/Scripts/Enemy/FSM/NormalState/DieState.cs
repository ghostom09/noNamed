using UnityEngine;

public class DieState : IState
{
    private Enemy _enemy;
    private bool _isSuicideBomber;

    public DieState(Enemy enemy, bool isSuicideBomber = false)
    {
        _enemy = enemy;
        _isSuicideBomber = isSuicideBomber;
    }

    public void Enter()
    {
        _enemy.rb.linearVelocity = Vector2.zero;
        Object.Destroy(_enemy.gameObject, 1f);
        _enemy.GetComponent<Collider2D>().enabled = false; // 피격 판정 제거
        
        if (_isSuicideBomber)
        {
            _enemy.Explode();
        }
        else
        {
            // 일반 사망 애니메이션
        }
        _enemy.enabled = false;           // AI/이동 중단
    }

    public void Update()
    {
        
    }

    public void Exit()
    {
        
    }
}