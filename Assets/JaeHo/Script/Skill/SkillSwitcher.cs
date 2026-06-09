using UnityEngine;
using UnityEngine.InputSystem;

public class SkillSwitcher : MonoBehaviour
{
    [SerializeField] private SkillBase[] skills;

    private int _currentIndex = 0;
    private SkillBase _currentSkill;
    private bool _attackHeld;
    private bool _attackPressed;
    private bool _attackReleased;

    private void Awake()
    {
        if (skills.Length == 0)
        {
            Debug.LogError("WeaponSwitcher에 무기가 등록되지 않음");
            return;
        }
        
        foreach (var weapon in skills)
            weapon.gameObject.SetActive(false);

        EquipWeapon(0);
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
        int newIndex = (_currentIndex + direction + skills.Length) % skills.Length;
        EquipWeapon(newIndex);
    }

    private void EquipWeapon(int index)
    {
        if (_currentSkill != null)
        {
            _currentSkill.OnUnequip();
            _currentSkill.gameObject.SetActive(false);
        }

        _currentIndex = index;
        _currentSkill = skills[_currentIndex];
        _currentSkill.gameObject.SetActive(true);
        _currentSkill.OnEquip();

        Debug.Log($"무기 교체 : {_currentSkill.name}");
    }
}
