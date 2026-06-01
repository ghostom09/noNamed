using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SkillA - 돌격소총.
/// 마우스 좌클릭 홀드로 연사. 발사 방향은 마우스 포인터 기준.
/// </summary>
public class SkillA : ProjectileSkill
{
    private ParticleSystem _muzzleFlash;

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

        if (!Mouse.current.leftButton.isPressed) return;
        if (AttackTimer < attackCooldown) return;

        AttackTimer = 0f;
        ExecuteAttack();
    }

    private void ExecuteAttack()
    {
        Vector2 direction = GetMouseDirection();
        RotateFirePoint(direction);
        PlayMuzzleFlash(direction);
        FireBullet(firePoint.position, direction);
    }

    // ── 헬퍼 ────────────────────────────────────────────────────

    private Vector2 GetMouseDirection()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = MainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y,
                Mathf.Abs(MainCamera.transform.position.z)));
        mouseWorldPos.z = 0f;

        return ((Vector2)mouseWorldPos - (Vector2)firePoint.position).normalized;
    }

    private void RotateFirePoint(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.Euler(0f, 0f, angle);
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