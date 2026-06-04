using UnityEngine;

public interface IKnockbackable
{
    void ApplyKnockback(Vector2 direction, float impulse, float collisionDamage, float extraTargetDamage, bool stunOnCollision);
}
