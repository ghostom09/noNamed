using UnityEngine;
using BossSystem.Boss.FleshBoss;

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

        if ((typeof(T) == typeof(IDamageable) || typeof(T) == typeof(IHitPointStatus)) &&
            TryGetFleshChunkAdapter(collider, out JunMoFleshChunkCombatAdapter fleshChunkAdapter))
        {
            component = fleshChunkAdapter as T;
            return component != null;
        }

        if (collider.TryGetComponent(out component))
            return true;

        component = collider.GetComponentInParent<T>();
        if (component != null)
            return true;

        if (collider.CompareTag("Player") && collider.transform.root != null)
        {
            component = collider.transform.root.GetComponentInChildren<T>();
            return component != null;
        }

        return false;
    }

    private static bool TryGetFleshChunkAdapter(Collider2D collider, out JunMoFleshChunkCombatAdapter adapter)
    {
        adapter = null;
        if (collider == null)
            return false;

        FleshChunk chunk = collider.GetComponentInParent<FleshChunk>();
        if (chunk == null)
            return false;

        adapter = chunk.GetComponent<JunMoFleshChunkCombatAdapter>();
        if (adapter == null)
            adapter = chunk.gameObject.AddComponent<JunMoFleshChunkCombatAdapter>();

        return adapter != null;
    }
}
