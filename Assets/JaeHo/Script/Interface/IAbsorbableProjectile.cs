using UnityEngine;

public interface IAbsorbableProjectile
{
    void Absorb(GameObject absorber);
    void Reflect(GameObject reflector, Vector2 direction, float damageMultiplier);
}
