using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    [Header("--- Base Skill Settings ---")]
    [SerializeField] protected float attackCooldown = 1.0f;
    [SerializeField] protected float attackDistance = 100f;
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected LayerMask targetLayer;
    [SerializeField] protected Transform firePoint;

    [Header("--- Tag Settings ---")]
    [SerializeField] private SkillTag defaultTags = SkillTag.None;  // 인스펙터에서 기본 태그 설정

    protected float AttackTimer;
    protected Camera MainCamera;

    // 현재 활성화된 태그 (defaultTags로 초기화)
    private SkillTag _currentTags;

    // 읽기 전용 프로퍼티 - 외부에서 태그 확인용
    public SkillTag CurrentTags => _currentTags;

    protected virtual void Awake()
    {
        MainCamera = Camera.main;
        if (firePoint == null) firePoint = transform;
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

    // ── 스킬 인터페이스 ─────────────────────────────────────────

    public abstract void OnAttack();

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }
}