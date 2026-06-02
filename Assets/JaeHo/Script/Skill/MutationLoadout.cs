using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MutationLoadout : MonoBehaviour
{
    [Header("--- Definitions ---")]
    [SerializeField] private MutationDefinition[] definitions = Array.Empty<MutationDefinition>();

    [Header("--- Runtime Stacks ---")]
    [SerializeField] private List<MutationStack> stacks = new();

    private readonly Dictionary<MutationType, MutationDefinition> _definitionMap = new();
    private readonly Dictionary<MutationType, MutationStack> _stackMap = new();

    public event Action<MutationType, int, MutationGrade> MutationChanged;

    private void Awake()
    {
        RebuildCache();
    }

    public int GetStackCount(MutationType mutationType)
    {
        return _stackMap.TryGetValue(mutationType, out var stack) ? stack.StackCount : 0;
    }

    public bool HasMutation(MutationType mutationType)
    {
        return GetStackCount(mutationType) > 0;
    }

    public MutationGrade GetGrade(MutationType mutationType)
    {
        int stackCount = GetStackCount(mutationType);

        if (TryGetDefinition(mutationType, out var definition))
            return definition.ResolveGrade(stackCount);

        return MutationGradeUtility.ResolveDefaultGrade(stackCount);
    }

    public bool TryGetDefinition(MutationType mutationType, out MutationDefinition definition)
    {
        return _definitionMap.TryGetValue(mutationType, out definition) && definition != null;
    }

    public bool TryGetGradeData(MutationType mutationType, out MutationGradeData gradeData)
    {
        gradeData = null;

        return TryGetDefinition(mutationType, out var definition) &&
               definition.TryGetGradeData(GetStackCount(mutationType), out gradeData);
    }

    public void AddStack(MutationType mutationType, int amount = 1)
    {
        if (amount == 0) return;

        MutationStack stack = GetOrCreateStack(mutationType);
        stack.AddStacks(amount);
        NotifyChanged(mutationType);
    }

    public void SetStackCount(MutationType mutationType, int stackCount)
    {
        MutationStack stack = GetOrCreateStack(mutationType);
        stack.SetStackCount(stackCount);
        NotifyChanged(mutationType);
    }

    public void Clear()
    {
        for (int i = 0; i < stacks.Count; i++)
            stacks[i]?.SetStackCount(0);

        RebuildCache();

        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i] != null)
                NotifyChanged(stacks[i].MutationType);
        }
    }

    public bool CanApplyToAttack(MutationType mutationType, AttackContext context)
    {
        return TryGetDefinition(mutationType, out var definition) &&
               definition.CanApplyTo(context.RangeType);
    }

    private MutationStack GetOrCreateStack(MutationType mutationType)
    {
        if (_stackMap.TryGetValue(mutationType, out var stack))
            return stack;

        stack = new MutationStack(mutationType, 0);
        stacks.Add(stack);
        _stackMap[mutationType] = stack;
        return stack;
    }

    private void NotifyChanged(MutationType mutationType)
    {
        MutationChanged?.Invoke(mutationType, GetStackCount(mutationType), GetGrade(mutationType));
    }

    private void RebuildCache()
    {
        _definitionMap.Clear();
        _stackMap.Clear();

        if (definitions != null)
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                MutationDefinition definition = definitions[i];
                if (definition == null) continue;

                _definitionMap[definition.MutationType] = definition;
            }
        }

        for (int i = stacks.Count - 1; i >= 0; i--)
        {
            MutationStack stack = stacks[i];
            if (stack == null)
            {
                stacks.RemoveAt(i);
                continue;
            }

            _stackMap[stack.MutationType] = stack;
        }
    }

    private void OnValidate()
    {
        for (int i = stacks.Count - 1; i >= 0; i--)
        {
            if (stacks[i] == null)
                stacks.RemoveAt(i);
            else
                stacks[i].SetStackCount(stacks[i].StackCount);
        }
    }
}
