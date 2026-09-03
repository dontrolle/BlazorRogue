using System;
using BlazorRogue.World;

namespace BlazorRogue.AI;

class SimpleAIComponent(Map map) : AIComponent(map)
{
    public const string ComponentId = "simple_ai";

    public override void TakeTurn()
    {
        if (!Awake)
        {
            // Map.DebugInfo.Add("Monster wasn't awake, so skipping.");
            return;
        }

        int dx = Math.Sign(map.Player.X - Owner!.X);
        int dy = Math.Sign(map.Player.Y - Owner.Y);

        int destX = Owner.X + dx;
        int destY = Owner.Y + dy;

        // Lava (and any future instakill liquid) is treated as impassable for pathing - the AI has
        // no terrain awareness yet, so this just stops monsters walking to their death.
        if (!map.IsBlocked(destX, destY) && !map.IsLethalLiquid(destX, destY))
        {
            // Trying to leave a slow liquid can fail - the turn is spent standing still.
            if (map.LiquidStumble(Owner!))
            {
                return;
            }

            // where we came from is definetely not blocking anymore, since we just vacated the tile
            map.BlocksMovementMap[Owner!.X, Owner.Y] = false;
            // do the move
            Owner.Move(dx, dy);
            // and we need to update blocked status for the destination tile (for the benefit of other moveables)
            map.BlocksMovementMap[destX, destY] = true;
        }
        else
        {
            if (map.Player.X == destX && map.Player.Y == destY)
            {
                bool hit = map.Game.FightingSystem.CloseCombatAttack(
                    Owner.CombatComponent!,
                    map.Player.CombatComponent!
                );
                References.SoundManager.PlayCombatSound(hit);
            }
        }
    }
}
