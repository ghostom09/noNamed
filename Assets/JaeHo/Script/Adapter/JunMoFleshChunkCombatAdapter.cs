using System.Reflection;
using BossSystem.Boss.FleshBoss;
using UnityEngine;

[DisallowMultipleComponent]
public class JunMoFleshChunkCombatAdapter : MonoBehaviour, IDamageable, IHitPointStatus
{
    private const string EnemyLayerName = "Enemy";

    private static readonly FieldInfo ChunkHpField = typeof(FleshChunk).GetField(
        "chunkHP",
        BindingFlags.Instance | BindingFlags.NonPublic);

    [SerializeField] private FleshChunk chunk;
    [SerializeField] private bool syncColliderLayersToEnemyLayer = true;

    public float CurrentHp => TryGetCurrentHp(out float currentHp) ? currentHp : 0f;
    public bool IsDead => chunk == null || chunk.IsDead;

    private void Awake()
    {
        BindReferences();
    }

    private void OnEnable()
    {
        BindReferences();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || chunk == null)
            return;

        float damage = Mathf.Max(0f, amount);
        float hpBefore = CurrentHp;
        chunk.TakeDamage(damage);
        float hpAfter = CurrentHp;

        Debug.Log($"[FleshChunk Hit] {gameObject.name} damage:{damage:0.##} hp:{hpBefore:0.##}->{hpAfter:0.##}", this);
    }

    private void BindReferences()
    {
        if (chunk == null)
            chunk = GetComponent<FleshChunk>();

        ConfigureHitDetectionLayer();
    }

    private bool TryGetCurrentHp(out float currentHp)
    {
        currentHp = 0f;
        if (chunk == null || ChunkHpField == null)
            return false;

        currentHp = Mathf.Max(0f, (float)ChunkHpField.GetValue(chunk));
        return true;
    }

    private void ConfigureHitDetectionLayer()
    {
        if (!syncColliderLayersToEnemyLayer)
            return;

        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        if (enemyLayer < 0)
            return;

        gameObject.layer = enemyLayer;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D targetCollider = colliders[i];
            if (targetCollider == null)
                continue;

            targetCollider.gameObject.layer = enemyLayer;
        }
    }
}
