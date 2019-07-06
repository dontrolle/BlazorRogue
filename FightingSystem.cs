using System;
using System.Diagnostics;

namespace BlazorRogue
{
    public class FightingSystem
    {
        public FightingSystem()
        {
        }

        public void CloseCombatAttack(CombatComponent attacker, CombatComponent defender)
        {
            var toHitRoll = Dice.RollD100();
            Debug.WriteLine($"toHitRoll {toHitRoll}");
            Debug.WriteLine($"attacker adv {attacker.Advantage}");
            var attackerSL = Dice.GetSuccessLevel(toHitRoll, attacker.WeaponSkill + attacker.Advantage);
            Debug.WriteLine($"att SL {attackerSL}");

            var toDefendRoll = Dice.RollD100();
            Debug.WriteLine($"defender adv {defender.Advantage}");
            var defenderSL = Dice.GetSuccessLevel(toDefendRoll, defender.WeaponSkill + defender.Advantage);
            Debug.WriteLine($"defenderSL {defenderSL}");

            bool hit = false;
            var attackerSLAdvantage = attackerSL - defenderSL;
            if(attackerSLAdvantage > 0)
            {
                hit = true;
            }
            else if (attackerSLAdvantage == 0 && attacker.WeaponSkill > defender.WeaponSkill)
            {
                hit = true;
            }

            Debug.WriteLine($"hit? {hit}; SL {attackerSLAdvantage}");

            if (hit)
            {
                attacker.GainAdvantage();
                defender.ResetAdvantage();
                var hitLocation = Dice.ReverseD100(toHitRoll);
                Debug.WriteLine($"hitLocation {hitLocation}");

                var damage = attacker.WeaponDamage + attackerSLAdvantage;
                Debug.WriteLine($"damage {damage}");
                defender.ApplyDamage(damage);
            }
            else
            {
                defender.GainAdvantage();
                attacker.ResetAdvantage();
            }

            // TODO: If accrued no advantage this round, or end the round outnumbered, loose 1 adv

            // TODO: Handle criticals and fumbles
        }
    }
}
