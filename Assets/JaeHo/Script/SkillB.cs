using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SkillB - 저격총.
/// 마우스 좌클릭 1회당 1발. 긴 쿨다운, 높은 데미지.
/// Piercing 태그를 인스펙터의 defaultTags에서 기본 설정.
/// (SkillBase.defaultTags = SkillTag.Piercing)
/// </summary>
public class SkillB : ProjectileSkill
{
    private ParticleSystem _muzzleFlash;

    // 좌클릭 단발 처리를 위한 플래그
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

        // 버튼을 뗐을 때 플래그 초기화 → 단발 보장
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