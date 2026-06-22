public enum EnemyGrade
{
    Normal,
    Elite
}

public enum EnemyFamily
{
    Soldier,
    Mecha,
    Researcher
}

public static class EliteEnemyModifier
{
    public static void Apply(EnemyStats stats)
    {
        switch (GetEnemyFamily(stats.attackType))
        {
            case EnemyFamily.Soldier:
                stats.maxHealth *= 2f;
                stats.damage *= 1.25f;
                stats.attackSpeed *= 0.8f;
                break;

            case EnemyFamily.Mecha:
                stats.maxHealth *= 1.5f;
                stats.damage *= 1.5f;
                stats.moveSpeed *= 1.2f;
                break;

            case EnemyFamily.Researcher:
                stats.maxHealth *= 2f;
                stats.damage *= 1.25f;
                stats.silenceDuration *= 1.5f;
                break;
        }
    }

    private static EnemyFamily GetEnemyFamily(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.SoldierSpread:
            case AttackType.SoldierFive:
            case AttackType.SoldierThree:
            case AttackType.SoldierContinue:
                return EnemyFamily.Soldier;

            case AttackType.MechaDash:
            case AttackType.MechaBoom:
            case AttackType.MechaWind:
            case AttackType.MechaBullet:
                return EnemyFamily.Mecha;

            case AttackType.Researcher:
            case AttackType.HighResearcher:
                return EnemyFamily.Researcher;

            default:
                return EnemyFamily.Soldier;
        }
    }
}
