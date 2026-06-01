using UnityEngine;

public class MechaDashAttackHitBox : MonoBehaviour
{
    private Enemy _enemy;
    private bool _isDashing;
 
    public void Init(Enemy enemy) => _enemy = enemy;
    public void SetDashing(bool value) => _isDashing = value;
 
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isDashing) return;
        if (!other.CompareTag("Player")) return;
 
        // other.GetComponent<PlayerStats>()?.TakeDamage(_enemy.stats.damage);
        Debug.Log("[Meka1] 돌진 데미지");
    }
}