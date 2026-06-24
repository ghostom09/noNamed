using UnityEngine;

[CreateAssetMenu(menuName = "Mutation/Mutation Data")]
public class MutationData : ScriptableObject
{
    private static readonly string[] MutationTypeNames =
    {
        "Ricochet",
        "FollowUp",
        "Spread",
        "Pierce",
        "Homing",
        "ProjectileAbsorb",
        "Knockback",
        "Stun",
        "Slow",
        "Bind",
        "Explosion",
        "Poison"
    };

    private static readonly string[] MutationGradeNames =
    {
        "None",
        "Safe",
        "Caution",
        "Danger",
        "Quarantine"
    };

    public string mutationName;
    [Tooltip("Matches feat/player-attack MutationType enum value.")]
    public int mutationType;
    [Tooltip("Display grade. Matches feat/player-attack MutationGrade enum value.")]
    public int grade = 1;
    [Min(1)] public int stackAmount = 1;
    [Tooltip("0 Common, 1 RangedOnly, 2 MeleeOnly.")]
    public int targetScope;

    [TextArea]
    public string targetConditionText;

    [TextArea]
    public string descriptionText;

    [TextArea]
    public string currentEffectText;

    [TextArea]
    public string evolutionText;

    public string MutationTypeName => GetName(MutationTypeNames, mutationType, "Unknown");
    public string GradeName => GetName(MutationGradeNames, grade, "Unknown");

    public bool CanApplyTo(Component skill)
    {
        if (skill == null)
        {
            return false;
        }

        return targetScope == 0 ||
               targetScope == 1 && IsOrInherits(skill.GetType(), "ProjectileSkill") ||
               targetScope == 2 && IsOrInherits(skill.GetType(), "MeleeSkill");
    }

    private static string GetName(string[] names, int index, string fallback)
    {
        return index >= 0 && index < names.Length ? names[index] : fallback;
    }

    private static bool IsOrInherits(System.Type type, string typeName)
    {
        while (type != null)
        {
            if (type.Name == typeName)
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }
}
