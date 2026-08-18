using System;

namespace BlazorRogue.Combat.Warhammer;

class FightingSystem(Game game) : IFightingSystem
{
    public Game Game { get; } = game;

    public bool CloseCombatAttack(CombatComponent attacker, CombatComponent defender)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);

        int toHitRoll = Dice.RollD100();

        int attackerSL = Dice.GetSuccessLevel(toHitRoll, attacker.WeaponSkill + attacker.Advantage);

        int toDefendRoll = Dice.RollD100();
        int defenderSL = Dice.GetSuccessLevel(
            toDefendRoll,
            defender.WeaponSkill + defender.Advantage
        );

        bool hit = false;
        int attackerSLAdvantage = attackerSL - defenderSL;
        if (attackerSLAdvantage > 0)
        {
            hit = true;
        }
        else if (attackerSLAdvantage == 0 && attacker.WeaponSkill > defender.WeaponSkill)
        {
            hit = true;
        }

        int damage = 0;
        if (hit)
        {
            attacker.GainAdvantage();
            defender.ResetAdvantage();

            damage = attacker.WeaponDamage + attackerSLAdvantage;
            defender.ApplyDamage(damage);
        }
        else
        {
            defender.GainAdvantage();
            attacker.ResetAdvantage();
        }

        string description = hit ? "hits" : "misses";
        string damageDescription = damage > 0 ? $" and deals {damage} damage." : "";
        Game.AddMessage(
            $"{attacker.Owner!.Name} {description} {defender.Owner!.Name}{damageDescription}"
        );

        if (Game.DebugMode)
        {
            Game.AddMessage(
                $"({attacker.Owner!.Name} rolls {toHitRoll} => SL {attackerSL}) ({defender.Owner!.Name} rolls {toDefendRoll} => SL {defenderSL}) (resulting SL for attacker: {attackerSLAdvantage})"
            );
        }

        return hit;
    }
}
