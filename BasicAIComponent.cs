using System;

namespace BlazorRogue
{
    public class RandomWalkAIComponent : AIComponent
    {
        Random random = new Random();

        public RandomWalkAIComponent(Map map) : base(map)
        {
        }

        // random walk
        public override void TakeTurn()
        {
            var dx = random.Next(-1, 2);
            var dy = random.Next(-1, 2);

            int destX = Owner.x + dx;
            int destY = Owner.y + dy;

            if (!Map.IsBlocked(destX, destY))
            {
                // where we came from is definetely not blocking anymore, since we just vacated the tile
                Map.BlocksMovementMap[Owner.x, Owner.y] = false;
                // do the move
                Owner.Move(dx, dy);
                // and we need to update blocked status for the destination tile (for the benefit of other moveables)
                Map.BlocksMovementMap[destX, destY] = true;
            }
        }
    }
}
