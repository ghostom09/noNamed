using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SkillA - 돌격소총.
/// 마우스 좌클릭 홀드로 연사.
/// FirePoint 회전은 FirePointRotator가 담당하므로 여기선 방향만 읽음.
/// </summary>
public class SkillA : ProjectileSkill
{
    private ParticleSystem _muzzleFlash;

    protected override void Awake()
    {
        base.Awake();
        if (firePoint == null) return;

        _muzzleFlash = firePoint.GetComponent<ParticleSystem>();

        if (_muzzleFlash == null)
            Debug.LogError($"[{name}] firePoint에 ParticleSystem 컴포넌트가 없음");
    }

    public override void OnAttack()
    {
        AttackTimer += Time.deltaTime;

        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.isPressed) return;
        if (AttackTimer < attackCooldown) return;

        AttackTimer = 0f;
        ExecuteAttack();
    }

    private void ExecuteAttack()
    {
        if (firePoint == null) return;

        // FirePointRotator가 이미 회전시켜뒀으므로 right 방향만 읽으면 됨
        Vector2 direction = firePoint.right;

        PlayMuzzleFlash(direction);
        FireBullet(firePoint.position, direction);
    }

    private void PlayMuzzleFlash(Vector2 direction)
    {
        if (_muzzleFlash == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        var main = _muzzleFlash.main;
        main.startRotation = -angle * Mathf.Deg2Rad;
        _muzzleFlash.Emit(1);
    }
}
