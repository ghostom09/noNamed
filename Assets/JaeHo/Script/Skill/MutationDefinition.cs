using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MutationDefinition", menuName = "SkillSystem/Mutation Definition")]
public sealed class MutationDefinition : ScriptableObject
{
    [SerializeField] private MutationType mutationType;
    [SerializeField] private MutationTargetScope targetScope = MutationTargetScope.Common;
    [SerializeField] private MutationGradeData[] grades = Array.Empty<MutationGradeData>();

    public MutationType MutationType => mutationType;
    public MutationTargetScope TargetScope => targetScope;
    public IReadOnlyList<MutationGradeData> Grades => grades;

    public bool CanApplyTo(AttackRangeType rangeType)
    {
        return targetScope == MutationTargetScope.Common ||
               targetScope == MutationTargetScope.RangedOnly && rangeType == AttackRangeType.Ranged ||
               targetScope == MutationTargetScope.MeleeOnly && rangeType == AttackRangeType.Melee;
    }

    public MutationGrade ResolveGrade(int stackCount)
    {
        return TryGetGradeData(stackCount, out var gradeData) ? gradeData.Grade : MutationGrade.None;
    }

    public bool TryGetGradeData(int stackCount, out MutationGradeData gradeData)
    {
        gradeData = null;
        if (stackCount <= 0 || grades == null) return false;

        for (int i = 0; i < grades.Length; i++)
        {
            MutationGradeData current = grades[i];
            if (current == null) continue;
            if (stackCount < current.RequiredStacks) continue;

            if (gradeData == null || current.RequiredStacks > gradeData.RequiredStacks)
                gradeData = current;
        }

        return gradeData != null;
    }

    public bool TryGetGradeData(MutationGrade grade, out MutationGradeData gradeData)
    {
        gradeData = null;
        if (grade == MutationGrade.None || grades == null) return false;

        for (int i = 0; i < grades.Length; i++)
        {
            if (grades[i] == null || grades[i].Grade != grade) continue;

            gradeData = grades[i];
            return true;
        }

        return false;
    }

    private void OnValidate()
    {
        if (grades == null) return;

        for (int i = 0; i < grades.Length; i++)
            grades[i]?.Validate();

        Array.Sort(grades, CompareGradeData);
    }

    private static int CompareGradeData(MutationGradeData left, MutationGradeData right)
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left == null) return 1;
        if (right == null) return -1;

        return left.RequiredStacks.CompareTo(right.RequiredStacks);
    }
}
