using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SkillC - 부채꼴 근거리 공격.
/// 마우스 방향을 중심으로 설정한 각도(halfAngle) 범위 안의 적에게 데미지.
/// Physics2D.OverlapCircleAll + 각도 필터로 구현.
/// </summary>
public class SkillC : MeleeSkill
{
    [Header("--- Fan Attack Settings ---")]
    [Tooltip("부채꼴의 절반 각도. 60 = 총 120도 범위")]
    [SerializeField] private float halfAngle = 60f;

    public override void OnAttack()
    {
        AttackTimer += Time.deltaTime;

        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (AttackTimer < attackCooldown) return;

        AttackTimer = 0f;
        ExecuteAttack();
    }

    private void ExecuteAttack()
    {
        Vector2 origin = firePoint.position;
        Vector2 forward = GetMouseDirection(origin);

        // WidenRange 태그 시 범위 1.5배
        float radius = HasTag(SkillTag.WidenRange) ? attackRadius * 1.5f : attackRadius;

        Collider2D[] hits = GetTargetsInRadius(origin, radius);

        foreach (var hit in hits)
        {
            if (!IsInFOV(origin, hit.transform.position, forward, halfAngle)) continue;
            ApplyDamage(hit);
        }
    }

    // ── 헬퍼 ────────────────────────────────────────────────────

    private Vector2 GetMouseDirection(Vector2 origin)
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = MainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y,
                Mathf.Abs(MainCamera.transform.position.z)));
        mouseWorldPos.z = 0f;

        return ((Vector2)mouseWorldPos - origin).normalized;
    }

    // ── 에디터 기즈모 (범위 확인용) ─────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);

        Vector2 origin = firePoint.position;
        // 에디터에서는 transform.right을 forward로 미리보기
        Vector2 forward = firePoint.right;

        int segments = 20;
        float angleStep = (halfAngle * 2f) / segments;
        float startAngle = -halfAngle;

        Vector3 prev = origin + RotateVector(forward, startAngle) * attackRadius;
        for (int i = 1; i <= segments; i++)
        {
            Vector3 next = (Vector2)origin +
                           RotateVector(forward, startAngle + angleStep * i) * attackRadius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        Gizmos.DrawLine(origin, (Vector2)origin + RotateVector(forward, -halfAngle) * attackRadius);
        Gizmos.DrawLine(origin, (Vector2)origin + RotateVector(forward, halfAngle) * attackRadius);
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }
#endif
}