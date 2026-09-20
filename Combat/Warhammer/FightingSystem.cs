using System;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

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
            string dealTerm = $"deal{(singularVerb ? "s" : "")}";
            string description = hit ? hitTerm : missTerm;
            string damageDescription = damage > 0 ? $" and {dealTerm} {damage} damage" : "";
            Game.AddMessage($"{attackerName} {description} {defenderName}{damageDescription}.");

            if (hit)
            {
                TryPushBack(
                    attacker.Owner!,
                    defender.Owner!,
                    attackerName,
                    defenderName,
                    singularVerb
                );
            }

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

    /// <summary>
    /// If <paramref name="attacker"/> has the push_back ability and the roll succeeds, shoves
    /// <paramref name="defender"/> one tile directly away from the attacker via the normal move
    /// pipeline (<see cref="GameObject.Move"/>, which <see cref="Moveable"/> overrides to chain
    /// into <see cref="World.Map.OnMoveableEnteredTile"/>) - so e.g. a shove into lava kills the
    /// defender the same way walking into it would. A blocked destination (a wall, another
    /// moveable, or - unless the defender is flying - a fence edge) fizzles the push without
    /// undoing the hit/damage already applied.
    /// </summary>
    void TryPushBack(
        GameObject attacker,
        GameObject defender,
        string attackerName,
        string defenderName,
        bool singularVerb
    )
    {
        var abilities = attacker.AbilitiesComponent;
        if (abilities?.Has(AbilityId.PushBack) != true)
        {
            return;
        }

        int chance = abilities.GetParameters(AbilityId.PushBack).GetInt("chance", 0);
        if (Dice.RollD100() > chance)
        {
            return;
        }

        var map = Game.Map;
        int dx = Math.Sign(defender.X - attacker.X);
        int dy = Math.Sign(defender.Y - attacker.Y);
        int destX = defender.X + dx;
        int destY = defender.Y + dy;

        string shoveTerm = $"shove{(singularVerb ? "s" : "")}";
        bool blocked =
            map.IsBlocked(destX, destY)
            || (
                map.IsMovementBlockedAcrossEdge(defender.X, defender.Y, destX, destY)
                && !World.Map.IsFlying(defender)
            );
        if (blocked)
        {
            Game.AddMessage(
                $"{attackerName} {shoveTerm} {defenderName} back, but there's nowhere to go!"
            );
            return;
        }

        int originX = defender.X;
        int originY = defender.Y;
        defender.Move(dx, dy);
        map.UpdateBlockMovement(originX, originY);
        map.UpdateBlockMovement(destX, destY);

        Game.AddMessage($"{attackerName} {shoveTerm} {defenderName} backward!");
    }
}
