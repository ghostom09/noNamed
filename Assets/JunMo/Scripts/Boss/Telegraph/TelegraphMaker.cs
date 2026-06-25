using BossSystem.Scripable;
using UnityEngine;

public class TelegraphMaker
{
    public Telegraph SpawnCircle(
        GameObject telegraphPrefab,
        Vector3 position,
        float radius,
        BossAttackData data,
        System.Action onComplete = null,
        Transform parent = null,
        float duration = -1f)
    {
        Telegraph telegraph = SpawnBase(telegraphPrefab, position, Quaternion.identity, data, onComplete, parent, duration);
        if (telegraph != null)
            SetWorldScale(telegraph.transform, new Vector3(radius * 2f, radius * 2f, 1f));

        return telegraph;
    }

    public Telegraph SpawnLine(
        GameObject telegraphPrefab,
        Vector3 startPosition,
        Vector2 direction,
        float length,
        float width,
        BossAttackData data,
        System.Action onComplete = null,
        float duration = -1f,
        bool anchorAtStart = false)
    {
        Vector2 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        Vector2 position = anchorAtStart
            ? (Vector2)startPosition
            : (Vector2)startPosition + dir * (length * 0.5f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        Telegraph telegraph = SpawnBase(
            telegraphPrefab,
            new Vector3(position.x, position.y, startPosition.z),
            Quaternion.Euler(0f, 0f, angle),
            data,
            onComplete,
            null,
            duration);

        if (telegraph != null)
            telegraph.SetLineSize(width, length);

        return telegraph;
    }

    private Telegraph SpawnBase(
        GameObject telegraphPrefab,
        Vector3 position,
        Quaternion rotation,
        BossAttackData data,
        System.Action onComplete,
        Transform parent = null,
        float duration = -1f)
    {
        if (telegraphPrefab == null)
        {
            Debug.LogWarning("[TelegraphMaker] Telegraph prefab is null.");
            onComplete?.Invoke();
            return null;
        }

        GameObject go = Object.Instantiate(telegraphPrefab, position, rotation, parent);
        Telegraph telegraph = go.GetComponent<Telegraph>();
        if (telegraph == null)
        {
            Debug.LogWarning($"[TelegraphMaker] {telegraphPrefab.name} does not have a Telegraph component.");
            onComplete?.Invoke();
            Object.Destroy(go);
            return null;
        }

        telegraph.SpawnTelegraph(data, onComplete, duration);
        return telegraph;
    }

    private static void SetWorldScale(Transform target, Vector3 worldScale)
    {
        if (target.parent == null)
        {
            target.localScale = worldScale;
            return;
        }

        Vector3 parentScale = target.parent.lossyScale;
        target.localScale = new Vector3(
            parentScale.x != 0f ? worldScale.x / parentScale.x : worldScale.x,
            parentScale.y != 0f ? worldScale.y / parentScale.y : worldScale.y,
            parentScale.z != 0f ? worldScale.z / parentScale.z : worldScale.z);
    }
}
