using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SkillTagDebugController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private SkillSwitcher skillSwitcher;

    [Header("Debug Tags")]
    [SerializeField] private SkillTag targetTags = SkillTag.None;

    private void Awake()
    {
        if (skillSwitcher != null) return;

        skillSwitcher = GetComponent<SkillSwitcher>();
        if (skillSwitcher == null)
            skillSwitcher = GetComponentInChildren<SkillSwitcher>();
        if (skillSwitcher == null)
            skillSwitcher = GetComponentInParent<SkillSwitcher>();
    }

    private void Update()
    {
        if (WasPressed(Key.F1))
            AddSelectedTags();

        if (WasPressed(Key.F2))
            RemoveSelectedTags();

        if (WasPressed(Key.F3))
            ResetCurrentSkillTags();

        if (WasPressed(Key.F4))
            LogCurrentSkillTags();
    }

    [ContextMenu("Add Selected Tags")]
    public void AddSelectedTags()
    {
        SkillBase skill = GetCurrentSkill();
        if (skill == null) return;

        skill.AddTag(targetTags);
        Debug.Log($"[SkillTagDebug] Add {targetTags} -> {skill.name}: {skill.CurrentTags}", skill);
    }

    [ContextMenu("Remove Selected Tags")]
    public void RemoveSelectedTags()
    {
        SkillBase skill = GetCurrentSkill();
        if (skill == null) return;

        skill.RemoveTag(targetTags);
        Debug.Log($"[SkillTagDebug] Remove {targetTags} -> {skill.name}: {skill.CurrentTags}", skill);
    }

    [ContextMenu("Reset Current Skill Tags")]
    public void ResetCurrentSkillTags()
    {
        SkillBase skill = GetCurrentSkill();
        if (skill == null) return;

        skill.ResetTags();
        Debug.Log($"[SkillTagDebug] Reset -> {skill.name}: {skill.CurrentTags}", skill);
    }

    [ContextMenu("Log Current Skill Tags")]
    public void LogCurrentSkillTags()
    {
        SkillBase skill = GetCurrentSkill();
        if (skill == null) return;

        Debug.Log($"[SkillTagDebug] Current -> {skill.name}: {skill.CurrentTags}", skill);
    }

    private SkillBase GetCurrentSkill()
    {
        if (skillSwitcher == null)
        {
            Debug.LogWarning("[SkillTagDebug] SkillSwitcher is not assigned.", this);
            return null;
        }

        SkillBase skill = skillSwitcher.CurrentSkill;
        if (skill == null)
            Debug.LogWarning("[SkillTagDebug] CurrentSkill is empty.", skillSwitcher);

        return skill;
    }

    private static bool WasPressed(Key key)
    {
        return key != Key.None
               && Keyboard.current != null
               && Keyboard.current[key].wasPressedThisFrame;
    }
}
