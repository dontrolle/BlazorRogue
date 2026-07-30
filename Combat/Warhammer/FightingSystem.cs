using System;
using System.Diagnostics;

namespace BlazorRogue.Combat.Warhammer;

class FightingSystem(Game game)
{
    public Game Game { get; } = game;

    public static bool CloseCombatAttack(CombatComponent attacker, CombatComponent defender)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);

        Debug.WriteLine($"----------------------------");
        int toHitRoll = Dice.RollD100();
        Debug.WriteLine($"{attacker.Owner!.Name} rolls {toHitRoll} and has adv {attacker.Advantage}");
        int attackerSL = Dice.GetSuccessLevel(toHitRoll, attacker.WeaponSkill + attacker.Advantage);
        Debug.WriteLine($" => SL {attackerSL}");

        int toDefendRoll = Dice.RollD100();
        Debug.WriteLine($"{defender.Owner!.Name} rolls {toDefendRoll} and has adv {defender.Advantage}");
        int defenderSL = Dice.GetSuccessLevel(toDefendRoll, defender.WeaponSkill + defender.Advantage);
        Debug.WriteLine($" => {defenderSL}");

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

        string description = hit ? "hit" : "miss";
        Debug.WriteLine($"Result: {description} with SL {attackerSLAdvantage}");

        if (hit)
        {
            attacker.GainAdvantage();
            defender.ResetAdvantage();
            int hitLocation = Dice.ReverseD100(toHitRoll);
            Debug.WriteLine($" hit location {hitLocation}");

            int damage = attacker.WeaponDamage + attackerSLAdvantage;
            Debug.WriteLine($" damage to apply {damage}");
            defender.ApplyDamage(damage);
        }
        else
        {
            defender.GainAdvantage();
            attacker.ResetAdvantage();
        }

        Debug.WriteLine($"----------------------------");
        return hit;

        // TODO: If accrued no advantage this round, or end the round outnumbered, loose 1 adv

        // TODO: Handle criticals and fumbles
    }
}
