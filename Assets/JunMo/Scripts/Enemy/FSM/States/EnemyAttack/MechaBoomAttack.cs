using System.Collections;
using UnityEngine;

public class MechaBoomAttack : IAttack
{
    private Enemy _enemy;
    
    public MechaBoomAttack(Enemy enemy)
    {
        _enemy = enemy;
    }
 
    public void Attack()
    {
        if (_enemy.IsAttacking) return;
        _enemy.StartCoroutine(Attacking());
    }

    public IEnumerator Attacking()
    {
        _enemy.IsAttacking = true;
        _enemy.AttackWarn();

        float timer = 0f;
        while (timer < _enemy.stats.durationWarning)
        {
            if (!_enemy)
                yield break;

            Vector2 nextPosition = (Vector2)_enemy.transform.position
                                   + _enemy.GetVector2() * (_enemy.stats.moveSpeed * Time.deltaTime);
            _enemy.rb.MovePosition(nextPosition);

            timer += Time.deltaTime;
            yield return null;
        }

        if (!_enemy)
            yield break;

        _enemy.Explode();
        _enemy.IsAttacking = false;
        _enemy.ChangeState(_enemy.DieState);
    }
}
