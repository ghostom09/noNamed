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
        _enemy.Explode();
        _enemy.ChangeState(_enemy.DieState);
    }
}