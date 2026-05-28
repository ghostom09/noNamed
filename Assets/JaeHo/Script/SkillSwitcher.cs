using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSwitcher : MonoBehaviour
{
    [SerializeField] private SkillBase[] skills;

    private int _currentIndex = 0;
    private SkillBase _currentSkill;

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
        if (Mouse.current == null) return;
        
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll > 0f) SwitchWeapon(-1);
        else if (scroll < 0f) SwitchWeapon(1);
        
        _currentSkill?.OnAttack();
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