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

        // if we're not in a test, then add messages
        if (Game != null)
        {
            var player = Game.Map.Player;
            bool attackerIsPlayer = ReferenceEquals(attacker.Owner, player);
            bool defenderIsPlayer = ReferenceEquals(defender.Owner, player);

            string attackerName = attackerIsPlayer ? "You" : $"The {attacker.Owner!.Name}";
            string defenderName = defenderIsPlayer ? "you" : $"the {defender.Owner!.Name}";

            // Second person ("You hit") takes no -s the way a third-person singular subject
            // ("The goblin hits") does.
            bool singularVerb = attacker.Owner!.Singular && !attackerIsPlayer;
            string hitTerm = $"hit{(singularVerb ? "s" : "")}";
            string missTerm = $"miss{(singularVerb ? "es" : "")}";
            string description = hit ? hitTerm : missTerm;
            string damageDescription = damage > 0 ? $" and deals {damage} damage" : "";
            Game.AddMessage($"{attackerName} {description} {defenderName}{damageDescription}.");

            if (Game.DebugMode)
            {
                string attackerRolls = attackerIsPlayer
                    ? "You roll"
                    : $"{attacker.Owner!.Name} rolls";
                string defenderRolls = defenderIsPlayer
                    ? "You roll"
                    : $"{defender.Owner!.Name} rolls";
                Game.AddMessage(
                    $"({attackerRolls} {toHitRoll} => SL {attackerSL}) ({defenderRolls} {toDefendRoll} => SL {defenderSL}) (resulting SL for attacker: {attackerSLAdvantage})"
                );
            }
        }

        return hit;
    }
}
