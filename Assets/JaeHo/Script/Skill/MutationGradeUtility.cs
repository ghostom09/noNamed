public static class MutationGradeUtility
{
    public static MutationGrade ResolveDefaultGrade(int stackCount)
    {
        return stackCount switch
        {
            <= 0 => MutationGrade.None,
            1 => MutationGrade.Safe,
            2 => MutationGrade.Caution,
            3 => MutationGrade.Danger,
            _ => MutationGrade.Quarantine
        };
    }
}
