using System;
using BlazorRogue.Combat.Warhammer;

namespace BlazorRogue.GameObjects
{
    public class Player : GameObject
    {
        public Player(int x, int y) : base(x, y, "Player", combatComponent: new CombatComponent(40, 8, 30, 3, 20))
        {
        }

        public override void Render(Map map)
        {
            map.MoveableDecorations[x, y].Add(new Decoration(this, null) { AnimationClass = "animated_templar" });
        }

        public override void Move(int xDelta, int yDelta)
        {
            base.Move(xDelta, yDelta);
            Game.SoundManager.PlayWalkSound();
        }
    }
}
