using UnityEngine;
using UnityEngine.InputSystem;

public class SkillSwitcher : MonoBehaviour
{
    [SerializeField] private SkillBase[] skills;

    private int _currentIndex;
    private SkillBase _currentSkill;
    private bool _attackHeld;
    private bool _attackPressed;
    private bool _attackReleased;

    public SkillBase CurrentSkill => _currentSkill;

    private void Awake()
    {
        if (!HasAnySkill())
        {
            Debug.LogError("[SkillSwitcher] No skills are assigned.", this);
            return;
        }

        foreach (SkillBase skill in skills)
        {
            if (skill != null)
                skill.gameObject.SetActive(false);
        }

        EquipWeapon(FindNextValidIndex(0, 1));
    }

    private void Update()
    {
        float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;

        if (scroll > 0f) SwitchWeapon(-1);
        else if (scroll < 0f) SwitchWeapon(1);

        _currentSkill?.OnAttack(new SkillInputState(_attackHeld, _attackPressed, _attackReleased));

        _attackPressed = false;
        _attackReleased = false;
    }

    public void SetAttackHeld(bool isHeld)
    {
        if (isHeld && !_attackHeld)
            _attackPressed = true;
        else if (!isHeld && _attackHeld)
            _attackReleased = true;

        _attackHeld = isHeld;
    }

    public void PressAttack()
    {
        SetAttackHeld(true);
    }

    public void ReleaseAttack()
    {
        SetAttackHeld(false);
    }

    public void SwitchNext()
    {
        SwitchWeapon(1);
    }

    public void SwitchPrevious()
    {
        SwitchWeapon(-1);
    }

    private void SwitchWeapon(int direction)
    {
        if (!HasAnySkill())
            return;

        int newIndex = FindNextValidIndex(_currentIndex + direction, direction);
        if (newIndex < 0 || newIndex == _currentIndex)
            return;

        EquipWeapon(newIndex);
    }

    private void EquipWeapon(int index)
    {
        if (index < 0 || skills == null || index >= skills.Length || skills[index] == null)
            return;

        if (_currentSkill != null)
        {
            _currentSkill.OnUnequip();
            _currentSkill.gameObject.SetActive(false);
        }

        ClearAttackState();
        _currentIndex = index;
        _currentSkill = skills[_currentIndex];
        _currentSkill.gameObject.SetActive(true);
        _currentSkill.OnEquip();

        Debug.Log($"Switched skill: {_currentSkill.name}");
    }

    private bool HasAnySkill()
    {
        if (skills == null || skills.Length == 0)
            return false;

        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i] != null)
                return true;
        }

        return false;
    }

    private int FindNextValidIndex(int startIndex, int direction)
    {
        if (skills == null || skills.Length == 0)
            return -1;

        int step = direction >= 0 ? 1 : -1;
        int index = Mod(startIndex, skills.Length);

        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[index] != null)
                return index;

            index = Mod(index + step, skills.Length);
        }

        return -1;
    }

    private void ClearAttackState()
    {
        _attackHeld = false;
        _attackPressed = false;
        _attackReleased = false;
    }

    private static int Mod(int value, int length)
    {
        return (value % length + length) % length;
    }
}
