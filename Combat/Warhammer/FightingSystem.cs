using System;
using System.Diagnostics;

namespace BlazorRogue.Combat.Warhammer
{
    public class FightingSystem
    {
        public Game Game { get; }

        public FightingSystem(Game game)
        {
            Game = game;
        }

        public bool CloseCombatAttack(CombatComponent attacker, CombatComponent defender)
        {
            Debug.WriteLine($"----------------------------");
            var toHitRoll = Dice.RollD100();
            Debug.WriteLine($"{attacker.Owner.Name} rolls {toHitRoll} and has adv {attacker.Advantage}");
            var attackerSL = Dice.GetSuccessLevel(toHitRoll, attacker.WeaponSkill + attacker.Advantage);
            Debug.WriteLine($" => SL {attackerSL}");

            var toDefendRoll = Dice.RollD100();
            Debug.WriteLine($"{defender.Owner.Name} rolls {toDefendRoll} and has adv {defender.Advantage}");
            var defenderSL = Dice.GetSuccessLevel(toDefendRoll, defender.WeaponSkill + defender.Advantage);
            Debug.WriteLine($" => {defenderSL}");

            var hit = false;
            var attackerSLAdvantage = attackerSL - defenderSL;
            if (attackerSLAdvantage > 0)
            {
                hit = true;
            }
            else if (attackerSLAdvantage == 0 && attacker.WeaponSkill > defender.WeaponSkill)
            {
                hit = true;
            }

            var description = hit ? "hit" : "miss";
            Debug.WriteLine($"Result: {description} with SL {attackerSLAdvantage}");

            if (hit)
            {
                attacker.GainAdvantage();
                defender.ResetAdvantage();
                var hitLocation = Dice.ReverseD100(toHitRoll);
                Debug.WriteLine($" hit location {hitLocation}");

                var damage = attacker.WeaponDamage + attackerSLAdvantage;
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
}
