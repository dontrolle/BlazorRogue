using System;

namespace BlazorRogue
{
    public abstract class Monster : GameObject
    {
        public string AnimationClass { get; protected set; }

        protected Monster(int x, int y, string name, AIComponent aIComponent, CombatComponent combatComponent, string animationClass = null) : base(x, y, name, aIComponent, combatComponent)
        {
            InvisibleOutsideFov = true;
            Blocking = true;
            AnimationClass = animationClass;

            // Note, can't block light due to the way moveables are treated in Map
        }

        public override void Render(Map map)
        {
            if (AnimationClass == null)
            {
                throw new InvalidOperationException("AnimationClass not set.");
            }

            map.MoveableDecorations[x, y].Add(new Decoration(this, null) { AnimationClass = AnimationClass });
        }

        public override void Move(int xDelta, int yDelta)
        {
            base.Move(xDelta, yDelta);
            Game.SoundManager.PlayWalkSound();
        }
    }
}
