using UnityEngine;

public class DieState : IState
{
    private readonly Enemy _enemy;
    private readonly bool _isSuicideBomber;

    public DieState(Enemy enemy, bool isSuicideBomber = false)
    {
        _enemy = enemy;
        _isSuicideBomber = isSuicideBomber;
    }

    public void Enter()
    {
        _enemy.Animation?.Play(EnemyAnimationType.Die);

        if (_enemy.TryStartChaseBeforeExplosion())
            return;

        StopAndDestroy();

        if (_isSuicideBomber && !_enemy.HasExploded)
            _enemy.Explode();

        _enemy.enabled = false;
    }

    public void Update()
    {
    }

    public void Exit()
    {
    }

    private void StopAndDestroy()
    {
        _enemy.rb.linearVelocity = Vector2.zero;
        Object.Destroy(_enemy.gameObject, 1f);

        Collider2D enemyCollider = _enemy.GetComponent<Collider2D>();
        if (enemyCollider != null)
            enemyCollider.enabled = false;
    }
}
