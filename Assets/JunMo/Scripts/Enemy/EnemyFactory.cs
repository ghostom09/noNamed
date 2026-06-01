using UnityEngine;
using System;

public static class EnemyFactory
{
    public static void Initialize(Enemy enemy, EnemyStats data)
    {
        GameObject prefab =
            EnemyPrefabController.Instance.GetPrefab(data.attackType);

        enemy.Chase = data.chaseType switch
        {
            ChaseType.MeleeMove  => new MeleeChase(enemy),
            ChaseType.MeleeShort => new MeleeChaseShort(enemy),
            ChaseType.Ranged     => new RangedChase(enemy),

            _ => throw new ArgumentOutOfRangeException(
                nameof(data.chaseType),
                $"ChaseType {data.chaseType} not assigned.")
        };

        enemy.Attack = data.attackType switch
        {
            AttackType.MechaDash       => new MechaDashAttack(enemy),
            AttackType.MechaBullet     => new MechaBulletAttack(enemy, prefab),
            AttackType.MechaWind       => new MechaWindAttack(enemy),
            AttackType.MechaBoom       => new MechaBoomAttack(enemy),

            AttackType.Researcher      => new ResearcherAttack(enemy, prefab),
            AttackType.HighResearcher  => new HighResearcherAttack(enemy, prefab),

            AttackType.SoldierContinue => new SoldierContinueAttack(enemy, prefab),
            AttackType.SoldierThree    => new SoldierThreeAttack(enemy, prefab),
            AttackType.SoldierFive     => new SoldierFiveAttack(enemy, prefab),
            AttackType.SoldierSpread   => new SoldierSpreadAttack(enemy, prefab),

            _ => throw new ArgumentOutOfRangeException(
                nameof(data.attackType),
                $"AttackType {data.attackType} not assigned.")
        };
    }
}