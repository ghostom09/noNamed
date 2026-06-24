using UnityEngine;

public static class CombatComponentUtility
{
    public static bool TryGet<T>(Collider2D collider, out T component) where T : class
    {
        component = null;
        if (collider == null) return false;

        if (typeof(T) == typeof(IDamageable) &&
            collider.CompareTag("Player") &&
            collider.GetComponentInParent<PlayerHealth>() is PlayerHealth playerHealth)
        {
            component = playerHealth as T;
            return component != null;
        }

        if (collider.TryGetComponent(out component))
            return true;

        component = collider.GetComponentInParent<T>();
        return component != null;
    }
}
