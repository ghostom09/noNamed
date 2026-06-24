public interface IAttackLockable
{
    bool CanAttack { get; }
    bool IsAttackLocked { get; }

    void ApplyAttackLock(float duration);
    void ClearAttackLock();
}
