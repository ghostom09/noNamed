using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SkillSelectSlotUI : MonoBehaviour
{
    private const string EmptyMutationListText = "\uBCF4\uC720 \uBCC0\uC774 \uC5C6\uC74C";

    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillDescriptionText;
    [SerializeField] private TMP_Text mutationListText;
    [SerializeField] private Image skillIconImage;
    [SerializeField] private Button selectButton;

    public void SetSkill(SkillData skillData, string mutationList)
    {
        SetSkill(
            skillData == null ? string.Empty : skillData.skillName,
            skillData == null ? string.Empty : skillData.skillDescription,
            skillData == null ? null : skillData.skillIcon,
            mutationList);
    }

    public void SetSkill(string skillName, string skillDescription, Sprite skillIcon, string mutationList)
    {
        SetText(skillNameText, skillName);
        SetText(skillDescriptionText, skillDescription);
        SetText(mutationListText, string.IsNullOrWhiteSpace(mutationList) ? EmptyMutationListText : mutationList);
        SetIcon(skillIcon);
    }

    public void SetInteractable(bool interactable)
    {
        if (selectButton != null)
        {
            selectButton.interactable = interactable;
        }
    }

    public void SetClickAction(UnityAction action)
    {
        if (selectButton == null)
        {
            return;
        }

        selectButton.onClick.RemoveAllListeners();

        if (action != null)
        {
            selectButton.onClick.AddListener(action);
        }
    }

    private void SetText(TMP_Text targetText, string value)
    {
        if (targetText != null)
        {
            targetText.text = value ?? string.Empty;
        }
    }

    private void SetIcon(Sprite icon)
    {
        if (skillIconImage == null)
        {
            return;
        }

        skillIconImage.sprite = icon;
        skillIconImage.enabled = icon != null;
    }
}
