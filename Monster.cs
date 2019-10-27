using System;
using BlazorRogue.Entities;

namespace BlazorRogue
{
    public class Monster : GameObject
    {
        public string AnimationClass { get; protected set; }
        public string Id { get; }
        public string AsciiCharacter { get; }
        public string AsciiColour { get; }

        public Monster(int x, int y, AIComponent aIComponent, MonsterType monsterType) : base(x, y, monsterType.Name, aIComponent, monsterType.CombatComponent)
        {
            AnimationClass = monsterType.AnimationClass;
            Id = monsterType.Id;
            AsciiCharacter = monsterType.AsciiCharacter;
            AsciiColour = monsterType.AsciiColour;
        }

        protected Monster(
            int x, 
            int y, 
            string name, 
            AIComponent aIComponent, 
            CombatComponent combatComponent, 
            string animationClass = null, 
            string id = "",
            string asciiCharacter = "",
            string asciiColour = "") : base(x, y, name, aIComponent, combatComponent)
        {
            InvisibleOutsideFov = true;
            Blocking = true;
            AnimationClass = animationClass;
            Id = id;
            AsciiCharacter = asciiCharacter;
            AsciiColour = asciiColour;

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
