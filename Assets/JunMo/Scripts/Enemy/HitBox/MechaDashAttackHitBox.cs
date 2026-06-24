using UnityEngine;

public class MechaDashAttackHitBox : MonoBehaviour
{
    private Enemy _enemy;
    private bool _isDashing;
    private bool _hasDamagedPlayer;
 
    public void Init(Enemy enemy) => _enemy = enemy;
    public void SetDashing(bool value)
    {
        _isDashing = value;
        if (value)
            _hasDamagedPlayer = false;
    }
 
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isDashing) return;
        if (_hasDamagedPlayer) return;
        if (!other.CompareTag("Player")) return;

        if (_enemy == null || _enemy.stats == null) return;
        if (!BossDamageUtility.TryDamagePlayer(other, _enemy.stats.damage)) return;

        _hasDamagedPlayer = true;
        Debug.Log($"[Meka1] 돌진 데미지 {_enemy.stats.damage}");
    }
}
