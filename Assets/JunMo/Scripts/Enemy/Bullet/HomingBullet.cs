using UnityEngine;

public class HomingBullet : BulletBase
{
    private Transform _target;
    private float _homingDuration;
    private float _elapsed;

    public void InitHoming(float damage, Transform target, float speed = 6f, float homingDuration = 3f)
    {
        base.Init(damage, Vector2.zero, speed);
        _target = target;
        _homingDuration = homingDuration;
    }

    protected override void Update()
    {
        _elapsed += Time.deltaTime;

        if (_target != null && _elapsed < _homingDuration)
        {
            direction = ((Vector2)_target.position - (Vector2)transform.position).normalized;
        }

        transform.Translate(speed * Time.deltaTime * direction, Space.World);
    }
}