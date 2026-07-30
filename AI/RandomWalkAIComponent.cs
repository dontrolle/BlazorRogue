using System;

namespace BlazorRogue.AI;

class RandomWalkAIComponent(Map map) : AIComponent(map)
{
    readonly Random random = new();

    public override void TakeTurn()
    {
        if (!Awake)
        {
            map.DebugInfo.Add("Monster wasn't awake, so skipping.");
            return;
        }

        int dx = random.Next(-1, 2);
        int dy = random.Next(-1, 2);

        int destX = Owner!.X + dx;
        int destY = Owner.Y + dy;

        if (!map.IsBlocked(destX, destY))
        {
            // where we came from is definetely not blocking anymore, since we just vacated the tile
            map.BlocksMovementMap[Owner.X, Owner.Y] = false;
            // do the move
            Owner.Move(dx, dy);
            // and we need to update blocked status for the destination tile (for the benefit of other moveables)
            map.BlocksMovementMap[destX, destY] = true;
        }
    }
}
