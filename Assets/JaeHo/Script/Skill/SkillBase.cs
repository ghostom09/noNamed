using System;
using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    [Header("--- Base Skill Settings ---")]
    [SerializeField] protected float attackCooldown = 1.0f;
    [SerializeField] protected float attackDistance = 100f;
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected LayerMask targetLayer;
    [SerializeField] protected Transform firePoint;

    [Header("--- Critical Settings ---")]
    [Tooltip("치명타 확률. 0 = 없음, 1 = 항상 치명타")]
    [SerializeField, Range(0f, 1f)] private float criticalChance = 0.1f;
    [Tooltip("치명타 시 데미지 배율")]
    [SerializeField, Min(1f)] private float criticalMultiplier = 1.5f;

    [Header("--- Tag Settings ---")]
    [SerializeField] private SkillTag defaultTags = SkillTag.None;  // 인스펙터에서 기본 태그 설정

    [Header("--- Mutation Settings ---")]
    [SerializeField] private MutationLoadout mutationLoadout;

    protected float AttackTimer;
    protected Camera MainCamera;

    // 현재 활성화된 태그 (defaultTags로 초기화)
    private SkillTag _currentTags;

    // 읽기 전용 프로퍼티 - 외부에서 태그 확인용
    public SkillTag CurrentTags => _currentTags;
    public MutationLoadout Mutations => mutationLoadout;
    public event Action<AttackHitResult> HitResolved;

    protected virtual void Awake()
    {
        MainCamera = Camera.main;
        if (firePoint == null)
            Debug.LogError($"[{name}] firePoint가 할당되지 않음 - 인스펙터에서 공용 FirePoint를 연결해주세요");

        if (mutationLoadout == null)
            mutationLoadout = GetComponentInParent<MutationLoadout>();
        
        AttackTimer = attackCooldown;
        _currentTags = defaultTags;
    }

    // ── 태그 관리 ───────────────────────────────────────────────

    /// <summary> 태그 추가. 이미 있으면 무시됨. </summary>
    public void AddTag(SkillTag tag)
    {
        _currentTags |= tag;
    }

    /// <summary> 태그 제거. 없는 태그를 제거해도 안전. </summary>
    public void RemoveTag(SkillTag tag)
    {
        _currentTags &= ~tag;
    }

    /// <summary> 특정 태그 보유 여부 확인. </summary>
    public bool HasTag(SkillTag tag)
    {
        return (_currentTags & tag) != 0;
    }

    /// <summary> 태그를 defaultTags 상태로 초기화. </summary>
    public void ResetTags()
    {
        _currentTags = defaultTags;
    }

    protected AttackContext CreateAttackContext(
        AttackRangeType rangeType,
        AttackShapeType shapeType,
        Vector2 origin,
        Vector2 direction)
    {
        GameObject attacker = transform.root != null ? transform.root.gameObject : gameObject;

        return new AttackContext(
            this,
            attacker,
            mutationLoadout,
            rangeType,
            shapeType,
            origin,
            direction,
            attackDamage,
            CurrentTags,
            targetLayer,
            criticalChance,
            criticalMultiplier);
    }

    public void ReportHitResult(AttackHitResult result)
    {
        HitResolved?.Invoke(result);
    }

    // ── 스킬 인터페이스 ─────────────────────────────────────────

    public abstract void OnAttack();

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }

    protected virtual void OnValidate()
    {
        criticalChance = Mathf.Clamp01(criticalChance);
        criticalMultiplier = Mathf.Max(1f, criticalMultiplier);
    }
}
