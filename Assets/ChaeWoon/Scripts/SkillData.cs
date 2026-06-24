using UnityEngine;

[CreateAssetMenu(menuName = "Skill/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillName;

    [TextArea]
    public string skillDescription;

    public Sprite skillIcon;
    public bool canApplyMutation;
}
