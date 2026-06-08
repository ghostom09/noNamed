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
        _enemy.enabled = false;           // AI/이동 중단
        _enemy.GetComponent<Collider>().enabled = false; // 피격 판정 제거
        
        Object.Destroy(_enemy.gameObject, 2f);
        if (_isSuicideBomber)
        {
            Explode();
        }
        else
        {
            // 일반 사망 애니메이션
        }
    }

    public void Update()
    {
        
    }

    public void Exit()
    {
        
    }

    private void Explode()
    {
        _enemy.ChangeState(_enemy.AttackState);
    }
}