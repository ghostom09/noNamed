using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class MutationSelectUI : MonoBehaviour
{
    public static MutationSelectUI Instance { get; private set; }

    private const int MaxSkillSlotCount = 4;
    private const string Title = "\uBCC0\uC774\uD560 \uACF5\uACA9\uC744 \uC120\uD0DD\uD558\uC2DC\uC624";
    private const string EmptyMutationListText = "\uBCF4\uC720 \uBCC0\uC774 \uC5C6\uC74C";

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private SkillSelectSlotUI[] skillSlots = new SkillSelectSlotUI[MaxSkillSlotCount];
    [FormerlySerializedAs("testSkills")]
    [SerializeField] private SkillData[] skillDisplayData = new SkillData[MaxSkillSlotCount];
    [SerializeField] private SkillMutationLoadoutBinder mutationLoadoutBinder;
    [SerializeField] private Component[] targetSkills = new Component[MaxSkillSlotCount];
    [SerializeField] private bool hideAfterSelection = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MutationSelectUI] Duplicate instance found.", this);
            return;
        }

        Instance = this;
        BindRuntimeReferences();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Show(MutationData mutationData)
    {
        BindRuntimeReferences();

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = Title;
        }

        for (int i = 0; i < MaxSkillSlotCount; i++)
        {
            SkillSelectSlotUI slot = GetSlot(i);

            if (slot == null)
            {
                continue;
            }

            SkillData skillData = GetSkillDisplayData(i);
            Component targetSkill = GetTargetSkill(i);
            string mutationList = mutationLoadoutBinder == null
                ? EmptyMutationListText
                : mutationLoadoutBinder.BuildMutationList(targetSkill);

            if (skillData != null)
            {
                slot.SetSkill(skillData, mutationList);
            }
            else
            {
                slot.SetSkill(
                    targetSkill == null ? string.Empty : targetSkill.name,
                    string.Empty,
                    null,
                    mutationList);
            }

            bool hasRuntimeSkill = targetSkill != null && mutationData != null && mutationData.CanApplyTo(targetSkill);
            bool hasDisplayOnlySkill = targetSkill == null && skillData != null && skillData.canApplyMutation;
            bool canApply = mutationData != null &&
                            (skillData == null || skillData.canApplyMutation) &&
                            (hasRuntimeSkill || hasDisplayOnlySkill);

            slot.SetInteractable(canApply);

            SkillData capturedSkillData = skillData;
            Component capturedSkill = targetSkill;
            MutationData capturedMutationData = mutationData;
            slot.SetClickAction(() => HandleSkillSlotClicked(capturedSkillData, capturedSkill, capturedMutationData));
        }
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void HandleSkillSlotClicked(SkillData skillData, Component targetSkill, MutationData mutationData)
    {
        if (mutationData == null)
        {
            Debug.LogWarning("[MutationSelectUI] MutationData is not assigned.", this);
            return;
        }

        if (targetSkill == null)
        {
            string displayOnlySkillName = skillData == null || string.IsNullOrWhiteSpace(skillData.skillName)
                ? "DisplayOnlySkill"
                : skillData.skillName;

            Debug.Log($"{displayOnlySkillName} \uC2A4\uD0AC \uC120\uD0DD - \uB7F0\uD0C0\uC784 \uACF5\uACA9 \uC2DC\uC2A4\uD15C\uC740 \uC544\uC9C1 \uC5F0\uACB0\uB418\uC9C0 \uC54A\uC74C", this);
            CloseAfterSelection(mutationData);
            return;
        }

        if (mutationLoadoutBinder == null)
        {
            Debug.LogWarning("[MutationSelectUI] SkillMutationLoadoutBinder was not found.", this);
            return;
        }

        if (!mutationLoadoutBinder.ApplyMutation(targetSkill, mutationData))
        {
            return;
        }

        string skillName = skillData == null || string.IsNullOrWhiteSpace(skillData.skillName)
            ? targetSkill.name
            : skillData.skillName;

        Debug.Log($"{skillName} \uC2A4\uD0AC\uC5D0 {mutationData.mutationName} \uBCC0\uC774 \uC801\uC6A9", this);
        CloseAfterSelection(mutationData);
    }

    private void CloseAfterSelection(MutationData mutationData)
    {
        if (hideAfterSelection)
        {
            Hide();
        }
        else
        {
            Show(mutationData);
        }
    }

    private SkillSelectSlotUI GetSlot(int index)
    {
        if (skillSlots == null || index < 0 || index >= skillSlots.Length)
        {
            return null;
        }

        return skillSlots[index];
    }

    private SkillData GetSkillDisplayData(int index)
    {
        if (skillDisplayData == null || index < 0 || index >= skillDisplayData.Length)
        {
            return null;
        }

        return skillDisplayData[index];
    }

    private Component GetTargetSkill(int index)
    {
        if (targetSkills != null && index >= 0 && index < targetSkills.Length && targetSkills[index] != null)
        {
            return targetSkills[index];
        }

        return mutationLoadoutBinder == null ? null : mutationLoadoutBinder.GetSkill(index);
    }

    private void BindRuntimeReferences()
    {
        if (mutationLoadoutBinder == null)
        {
            mutationLoadoutBinder = FindAnyObjectByType<SkillMutationLoadoutBinder>();
        }

        if (mutationLoadoutBinder != null)
        {
            return;
        }

        Component skill = SkillMutationLoadoutBinder.FindFirstComponentByTypeName("SkillBase");

        if (skill == null)
        {
            return;
        }

        GameObject owner = skill.transform.root == null
            ? skill.gameObject
            : skill.transform.root.gameObject;

        mutationLoadoutBinder = owner.GetComponent<SkillMutationLoadoutBinder>();

        if (mutationLoadoutBinder == null)
        {
            mutationLoadoutBinder = owner.AddComponent<SkillMutationLoadoutBinder>();
        }
    }
}
