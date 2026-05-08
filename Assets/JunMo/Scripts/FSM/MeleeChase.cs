using UnityEngine;

public class MeleeChase : IChase
{
    private Enemy _enemy;

    public MeleeChase(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Chase()
    {
        float angle = _enemy.GetAngle();
        float randomAngle = Random.Range(-15f, 15f);
        _enemy.transform.rotation = Quaternion.Euler(0f, 0f, angle + randomAngle);
        
    }
}