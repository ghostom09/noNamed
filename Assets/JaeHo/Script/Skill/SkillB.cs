using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SkillB - 저격총.
/// 단발. FirePoint 회전은 FirePointRotator가 담당.
/// defaultTags = Piercing 인스펙터에서 설정.
/// </summary>
public class SkillB : ProjectileSkill
{
    private ParticleSystem _muzzleFlash;
    private bool _fired;

    protected override void Awake()
    {
        base.Awake();
        _muzzleFlash = firePoint.GetComponent<ParticleSystem>();

        if (_muzzleFlash == null)
            Debug.LogError($"[{name}] firePoint에 ParticleSystem 컴포넌트가 없음");
    }

    public override void OnAttack()
    {
        AttackTimer += Time.deltaTime;

        if (!Mouse.current.leftButton.isPressed)
        {
            _fired = false;
            return;
        }

        if (_fired) return;
        if (AttackTimer < attackCooldown) return;

        AttackTimer = 0f;
        _fired = true;
        ExecuteAttack();
    }

    private void ExecuteAttack()
    {
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