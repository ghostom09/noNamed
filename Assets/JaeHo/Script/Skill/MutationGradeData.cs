using System;
using UnityEngine;

/// <summary>
/// 변이 한 종류의 특정 등급이 가지는 수치 데이터.
/// 실제 효과 코드는 이 값을 읽어서 도탄 횟수, 지속시간, 배율 등을 적용한다.
/// </summary>
[Serializable]
public sealed class MutationGradeData
{
    [SerializeField] private MutationGrade grade = MutationGrade.Safe;
    [SerializeField, Min(1)] private int requiredStacks = 1;
    [SerializeField] private MutationTriggerType triggerType = MutationTriggerType.OnHit;

    [Header("--- Common Values ---")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float bonusDamage = 0f;
    [SerializeField] private float duration = 0f;
    [SerializeField] private float tickInterval = 0f;
    [SerializeField] private int count = 0;

    [Header("--- Flags ---")]
    [SerializeField] private bool unlimited = false;
    [SerializeField] private bool stackable = false;

    [Header("--- Memo ---")]
    [TextArea(2, 4)]
    [SerializeField] private string description;

    public MutationGrade Grade => grade;
    public int RequiredStacks => requiredStacks;
    public MutationTriggerType TriggerType => triggerType;
    public float DamageMultiplier => damageMultiplier;
    public float BonusDamage => bonusDamage;
    public float Duration => duration;
    public float TickInterval => tickInterval;
    public int Count => count;
    public bool Unlimited => unlimited;
    public bool Stackable => stackable;
    public string Description => description;

    public void Validate()
    {
        if (grade == MutationGrade.None)
            grade = MutationGrade.Safe;

        requiredStacks = Mathf.Max(1, requiredStacks);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        duration = Mathf.Max(0f, duration);
        tickInterval = Mathf.Max(0f, tickInterval);
        count = Mathf.Max(0, count);
    }
}
