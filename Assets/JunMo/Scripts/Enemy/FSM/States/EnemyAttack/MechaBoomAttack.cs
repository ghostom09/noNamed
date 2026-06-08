using UnityEngine;

public class MechaBoomAttack : IAttack
{
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDamage = 50f;
    private Enemy _enemy;
    
    public MechaBoomAttack(Enemy enemy)
    {
        _enemy = enemy;
    }
 
    public void Attack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_enemy.transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // hit.GetComponent<PlayerStats>()?.TakeDamage(explosionDamage);
                Debug.Log("[SuicideMeka] 자폭 데미지");
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
 
        // other.GetComponent<PlayerStats>()?.TakeDamage(explosionDamage * 0.5f);
        Debug.Log("[SuicideMeka] 접촉 데미지");
    }
 
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_enemy.transform.position, explosionRadius);
    }
}