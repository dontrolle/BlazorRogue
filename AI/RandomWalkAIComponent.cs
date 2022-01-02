using System;

namespace BlazorRogue.AI
{
  public class RandomWalkAIComponent : AIComponent
  {
    readonly Random random = new Random();

    public RandomWalkAIComponent(Map map) : base(map)
    {
    }

    public override void TakeTurn()
    {
      if (!Awake)
      {
        Map.DebugInfo.Add("Monster wasn't awake, so skipping.");
        return;
      }


      var dx = random.Next(-1, 2);
      var dy = random.Next(-1, 2);

      var destX = Owner!.x + dx;
      var destY = Owner.y + dy;

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
