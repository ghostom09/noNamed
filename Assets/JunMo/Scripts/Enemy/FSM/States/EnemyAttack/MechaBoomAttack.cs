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
        yield return new WaitForSeconds(_enemy.stats.durationWarning);
        if (!_enemy)
            yield break;
        _enemy.Explode();
        _enemy.IsAttacking = false;
        _enemy.ChangeState(_enemy.DieState);
    }
}