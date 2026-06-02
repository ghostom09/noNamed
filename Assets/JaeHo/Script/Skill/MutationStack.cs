using System;
using UnityEngine;

[Serializable]
public sealed class MutationStack
{
    [SerializeField] private MutationType mutationType;
    [SerializeField, Min(0)] private int stackCount;

    public MutationType MutationType => mutationType;
    public int StackCount => stackCount;

    public MutationStack(MutationType mutationType, int stackCount)
    {
        this.mutationType = mutationType;
        this.stackCount = Mathf.Max(0, stackCount);
    }

    public void SetStackCount(int value)
    {
        stackCount = Mathf.Max(0, value);
    }

    public void AddStacks(int amount)
    {
        stackCount = Mathf.Max(0, stackCount + amount);
    }
}
