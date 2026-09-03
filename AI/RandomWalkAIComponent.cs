using System;
using BlazorRogue.World;

namespace BlazorRogue.AI;

class RandomWalkAIComponent(Map map) : AIComponent(map)
{
    public const string ComponentId = "random_walk_ai";

    readonly Random random = new();

    public override void TakeTurn()
    {
        if (!Awake)
        {
            return;
        }

        int dx = random.Next(-1, 2);
        int dy = random.Next(-1, 2);

        int destX = Owner!.X + dx;
        int destY = Owner.Y + dy;

        // Lava (and any future instakill liquid) is impassable for pathing - the AI has no terrain
        // awareness yet, so this just stops monsters wandering to their death.
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
    }
}
