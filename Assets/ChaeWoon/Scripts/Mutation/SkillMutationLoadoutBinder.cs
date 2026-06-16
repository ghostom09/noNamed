using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class SkillMutationLoadoutBinder : MonoBehaviour
{
    [SerializeField] private bool autoFindSkillsInChildren = true;
    [SerializeField] private bool addMissingLoadouts = true;
    [SerializeField] private Component[] skills = Array.Empty<Component>();

    private readonly List<Component> runtimeSkills = new();

    public int SkillCount
    {
        get
        {
            RebuildIfNeeded();
            return runtimeSkills.Count;
        }
    }

    private void Awake()
    {
        Rebuild();
    }

    private void Start()
    {
        EnsureSkillLoadouts();
    }

    public void Rebuild()
    {
        runtimeSkills.Clear();

        if (autoFindSkillsInChildren)
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (IsSkillComponent(behaviours[i]))
                {
                    runtimeSkills.Add(behaviours[i]);
                }
            }
        }
        else if (skills != null)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                if (IsSkillComponent(skills[i]))
                {
                    runtimeSkills.Add(skills[i]);
                }
            }
        }

        EnsureSkillLoadouts();
    }

    public Component GetSkill(int index)
    {
        RebuildIfNeeded();
        return index >= 0 && index < runtimeSkills.Count ? runtimeSkills[index] : null;
    }

    public Component GetLoadout(Component skill)
    {
        if (!IsSkillComponent(skill))
        {
            return null;
        }

        Type loadoutType = FindRuntimeType("MutationLoadout");

        if (loadoutType == null)
        {
            return null;
        }

        Component loadout = skill.GetComponent(loadoutType);

        if (loadout == null && addMissingLoadouts)
        {
            loadout = skill.gameObject.AddComponent(loadoutType);
        }

        InjectLoadout(skill, loadout);
        return loadout;
    }

    public bool ApplyMutation(Component skill, MutationData mutationData)
    {
        if (!IsSkillComponent(skill) || mutationData == null)
        {
            return false;
        }

        if (!mutationData.CanApplyTo(skill))
        {
            Debug.LogWarning($"[SkillMutationLoadoutBinder] {mutationData.mutationName} cannot apply to {skill.name}.", this);
            return false;
        }

        Component loadout = GetLoadout(skill);

        if (loadout == null)
        {
            Debug.LogWarning($"[SkillMutationLoadoutBinder] {skill.name} has no MutationLoadout.", skill);
            return false;
        }

        Type mutationType = FindRuntimeType("MutationType");

        if (mutationType == null || !mutationType.IsEnum)
        {
            Debug.LogWarning("[SkillMutationLoadoutBinder] MutationType enum was not found.", this);
            return false;
        }

        object mutationValue = Enum.ToObject(mutationType, mutationData.mutationType);
        MethodInfo addStack = loadout.GetType().GetMethod("AddStack", new[] { mutationType, typeof(int) });

        if (addStack == null)
        {
            Debug.LogWarning("[SkillMutationLoadoutBinder] MutationLoadout.AddStack was not found.", loadout);
            return false;
        }

        addStack.Invoke(loadout, new[] { mutationValue, Mathf.Max(1, mutationData.stackAmount) });
        return true;
    }

    public string BuildMutationList(Component skill)
    {
        Component loadout = GetLoadout(skill);

        if (loadout == null)
        {
            return string.Empty;
        }

        Type mutationType = FindRuntimeType("MutationType");

        if (mutationType == null || !mutationType.IsEnum)
        {
            return string.Empty;
        }

        MethodInfo getStackCount = loadout.GetType().GetMethod("GetStackCount", new[] { mutationType });
        MethodInfo getGrade = loadout.GetType().GetMethod("GetGrade", new[] { mutationType });

        if (getStackCount == null || getGrade == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        Array mutationValues = Enum.GetValues(mutationType);

        for (int i = 0; i < mutationValues.Length; i++)
        {
            object mutationValue = mutationValues.GetValue(i);
            int stackCount = Convert.ToInt32(getStackCount.Invoke(loadout, new[] { mutationValue }));

            if (stackCount <= 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder
                .Append(mutationValue)
                .Append(" x")
                .Append(stackCount)
                .Append(" (")
                .Append(getGrade.Invoke(loadout, new[] { mutationValue }))
                .Append(')');
        }

        return builder.ToString();
    }

    public static bool IsSkillComponent(Component component)
    {
        return component != null && IsOrInherits(component.GetType(), "SkillBase");
    }

    public static Type FindRuntimeType(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < assemblies.Length; i++)
        {
            Type[] types;

            try
            {
                types = assemblies[i].GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types;
            }

            if (types == null)
            {
                continue;
            }

            for (int j = 0; j < types.Length; j++)
            {
                Type type = types[j];

                if (type != null && type.Name == typeName)
                {
                    return type;
                }
            }
        }

        return null;
    }

    public static Component FindFirstComponentByTypeName(string typeName)
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && IsOrInherits(behaviours[i].GetType(), typeName))
            {
                return behaviours[i];
            }
        }

        return null;
    }

    public static bool HasComponentInParentByTypeName(Component component, string typeName)
    {
        if (component == null)
        {
            return false;
        }

        Transform current = component.transform;

        while (current != null)
        {
            if (HasComponentByTypeName(current, typeName))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void EnsureSkillLoadouts()
    {
        for (int i = 0; i < runtimeSkills.Count; i++)
        {
            GetLoadout(runtimeSkills[i]);
        }
    }

    private static void InjectLoadout(Component skill, Component loadout)
    {
        if (skill == null || loadout == null)
        {
            return;
        }

        FieldInfo field = skill.GetType().GetField("mutationLoadout", BindingFlags.Instance | BindingFlags.NonPublic);

        if (field != null && field.FieldType.IsInstanceOfType(loadout))
        {
            field.SetValue(skill, loadout);
        }
    }

    private void RebuildIfNeeded()
    {
        if (runtimeSkills.Count == 0)
        {
            Rebuild();
        }
    }

    private static bool HasComponentByTypeName(Transform target, string typeName)
    {
        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && IsOrInherits(behaviours[i].GetType(), typeName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOrInherits(Type type, string typeName)
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
