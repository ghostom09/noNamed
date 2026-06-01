using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SkillD - 전방 직선 근거리 공격.
/// 마우스 방향으로 BoxCast를 쏴서 범위 안의 적에게 데미지.
/// </summary>
public class SkillD : MeleeSkill
{
    [Header("--- Box Attack Settings ---")]
    [Tooltip("박스의 가로(타격 너비)")]
    [SerializeField] private float boxWidth = 1.5f;

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
        Vector2 direction = GetMouseDirection(origin);

        // WidenRange 태그 시 박스 너비 1.5배
        float width = HasTag(SkillTag.WidenRange) ? boxWidth * 1.5f : boxWidth;
        Vector2 boxSize = new Vector2(width, width);

        RaycastHit2D[] hits = GetTargetsInBox(origin, direction, boxSize, attackDistance);

        // MultiHit 없으면 첫 번째 타겟만 피격
        if (HasTag(SkillTag.MultiHit))
        {
            foreach (var hit in hits)
                ApplyDamage(hit.collider);
        }
        else
        {
            if (hits.Length > 0)
                ApplyDamage(hits[0].collider);
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

    // ── 에디터 기즈모 ────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);

        Vector2 origin = firePoint.position;
        Vector2 forward = firePoint.right;
        Vector2 endPos = origin + forward * attackDistance;

        // BoxCast 시각화: 시작과 끝에 박스 그리기
        DrawBox(origin, new Vector2(boxWidth, boxWidth));
        DrawBox(endPos, new Vector2(boxWidth, boxWidth));
        Gizmos.DrawLine(origin + Vector2.up * (boxWidth * 0.5f),
                        endPos + Vector2.up * (boxWidth * 0.5f));
        Gizmos.DrawLine(origin - Vector2.up * (boxWidth * 0.5f),
                        endPos - Vector2.up * (boxWidth * 0.5f));
    }

    private void DrawBox(Vector2 center, Vector2 size)
    {
        Vector2 half = size * 0.5f;
        Vector2 tl = center + new Vector2(-half.x, half.y);
        Vector2 tr = center + new Vector2(half.x, half.y);
        Vector2 bl = center + new Vector2(-half.x, -half.y);
        Vector2 br = center + new Vector2(half.x, -half.y);
        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
        Gizmos.DrawLine(bl, tl);
    }
#endif
}