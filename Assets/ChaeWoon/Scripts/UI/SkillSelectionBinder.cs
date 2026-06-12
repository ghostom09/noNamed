using System.Reflection;
using UnityEngine;

public class SkillSelectionBinder : MonoBehaviour
{
    [SerializeField] private Skill skillView;
    [SerializeField] private Component skillSwitcher;
    [SerializeField] private bool syncFromSwitcher = true;

    private bool syncingView;
    private int lastKnownSwitcherIndex = -1;

    private void Awake()
    {
        BindReferences();
    }

    private void OnEnable()
    {
        BindReferences();

        if (skillView != null)
        {
            skillView.CurrentSkillChanged += HandleUiSkillChanged;
        }

        SyncViewFromSwitcher();
    }

    private void LateUpdate()
    {
        if (syncFromSwitcher)
        {
            SyncViewFromSwitcher();
        }
    }

    private void OnDisable()
    {
        if (skillView != null)
        {
            skillView.CurrentSkillChanged -= HandleUiSkillChanged;
        }
    }

    public void EquipSkill(int index)
    {
        BindReferences();

        MethodInfo equipWeapon = skillSwitcher == null
            ? null
            : skillSwitcher.GetType().GetMethod("EquipWeapon", BindingFlags.Instance | BindingFlags.NonPublic);

        if (skillSwitcher == null || equipWeapon == null)
        {
            return;
        }

        equipWeapon.Invoke(skillSwitcher, new object[] { index });
        lastKnownSwitcherIndex = index;
    }

    private void HandleUiSkillChanged(int skillIndex)
    {
        if (syncingView || skillIndex < 0)
        {
            return;
        }

        EquipSkill(skillIndex);
    }

    private void SyncViewFromSwitcher()
    {
        BindReferences();

        FieldInfo currentIndex = skillSwitcher == null
            ? null
            : skillSwitcher.GetType().GetField("_currentIndex", BindingFlags.Instance | BindingFlags.NonPublic);

        if (skillView == null || skillSwitcher == null || currentIndex == null)
        {
            return;
        }

        int switcherIndex = (int)currentIndex.GetValue(skillSwitcher);

        if (switcherIndex == lastKnownSwitcherIndex && skillView.CurrentSkillIndex == switcherIndex)
        {
            return;
        }

        lastKnownSwitcherIndex = switcherIndex;
        syncingView = true;
        skillView.SetCurrentSkillSilently(switcherIndex);
        syncingView = false;
    }

    private void BindReferences()
    {
        if (skillView == null)
        {
            skillView = GetComponent<Skill>();
        }

        if (skillView == null)
        {
            skillView = FindAnyObjectByType<Skill>();
        }

        if (skillSwitcher == null)
        {
            skillSwitcher = SkillMutationLoadoutBinder.FindFirstComponentByTypeName("SkillSwitcher");
        }
    }
}
