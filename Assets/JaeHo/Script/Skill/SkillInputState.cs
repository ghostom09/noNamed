public readonly struct SkillInputState
{
    public readonly bool AttackHeld;
    public readonly bool AttackPressed;
    public readonly bool AttackReleased;

    public SkillInputState(bool attackHeld, bool attackPressed, bool attackReleased)
    {
        AttackHeld = attackHeld;
        AttackPressed = attackPressed;
        AttackReleased = attackReleased;
    }

    public static SkillInputState None => new(false, false, false);
}
