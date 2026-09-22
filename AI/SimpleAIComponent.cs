using System;
using BlazorRogue.World;

namespace BlazorRogue.AI;

class SimpleAIComponent(Map map) : AIComponent(map)
{
    public const string ComponentId = "simple_ai";

    public override AITurnOutcome TakeTurn()
    {
        if (!Awake)
        {
            return new AITurnOutcome.DidNothing();
        }

        int dx = Math.Sign(map.Player.X - Owner!.X);
        int dy = Math.Sign(map.Player.Y - Owner.Y);

        int destX = Owner.X + dx;
        int destY = Owner.Y + dy;

        // Lava (and any future instakill liquid) is treated as impassable for pathing - the AI has
        // no terrain awareness yet, so this just stops monsters walking to their death. A flying
        // monster is unaffected by lava and can cross a blocked edge (fence) it would otherwise
        // have to path around - see AbilityId.Flying.
        bool flying = Map.IsFlying(Owner);
        if (
            !map.IsBlocked(destX, destY)
            && (!map.IsLethalLiquid(destX, destY) || flying)
            && (!map.IsMovementBlockedAcrossEdge(Owner.X, Owner.Y, destX, destY) || flying)
        )
        {
            // Trying to leave a slow liquid can fail - the turn is spent standing still.
            if (map.LiquidStumble(Owner!))
            {
                return new AITurnOutcome.DidNothing();
            }

            // where we came from is definetely not blocking anymore, since we just vacated the tile
            map.BlocksMovementMap[Owner!.X, Owner.Y] = false;
            // do the move
            Owner.Move(dx, dy);
            // and we need to update blocked status for the destination tile (for the benefit of other moveables)
            map.BlocksMovementMap[destX, destY] = true;
            return new AITurnOutcome.Moved(Owner.X, Owner.Y);
        }
        // A blocked edge (e.g. a fence) blocks combat the same way it blocks movement - can't
        // reach through it to attack the player standing just beyond it.
        else if (
            map.Player.X == destX
            && map.Player.Y == destY
            && (!map.IsMovementBlockedAcrossEdge(Owner.X, Owner.Y, destX, destY) || flying)
        )
        {
            var result = map.Game.FightingSystem.CloseCombatAttack(
                Owner.CombatComponent!,
                map.Player.CombatComponent!
            );
            References.SoundManager.PlayCombatSound(result.Hit);
            return new AITurnOutcome.Attacked(result);
        }

        return new AITurnOutcome.DidNothing();
    }
}
