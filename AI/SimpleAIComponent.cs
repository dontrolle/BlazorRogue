using System;

namespace BlazorRogue.AI
{
    public class SimpleAIComponent : AIComponent
    {
        public const string ComponentId = "SimpleAIComponent";

        public SimpleAIComponent(Map map) : base(map)
        {
        }

        public override void TakeTurn()
        {
            if (!Awake)
            {
                // Map.DebugInfo.Add("Monster wasn't awake, so skipping.");
                return;
            }

            var dx = Math.Sign(Map.Player.x - Owner!.x);
            var dy = Math.Sign(Map.Player.y - Owner.y);

            var destX = Owner.x + dx;
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
            else
            {
                if (Map.Player.x == destX && Map.Player.y == destY)
                {
                    var hit = Map.Game.FightingSystem.CloseCombatAttack(Owner.CombatComponent!, Map.Player.CombatComponent!);
                    Game.SoundManager.PlayCombatSound(hit);
                }
            }
        }
    }
}
