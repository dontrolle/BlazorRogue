using System;
using BlazorRogue.Combat.Warhammer;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue
{
    public class Monster : GameObject
    {
        public string AnimationClass { get; protected set; }
        public string Id { get; }
        public string AsciiCharacter { get; }
        public string AsciiColour { get; }

        public Monster(int x, int y, AIComponent aIComponent, MonsterType monsterType) : 
            base(x, y, monsterType.Name, aIComponent, new CombatComponent(monsterType.WeaponSkill, monsterType.WeaponDamage, monsterType.Toughness, monsterType.Armour, monsterType.Wounds))
        {
            InvisibleOutsideFov = true;
            Blocking = true;
            AnimationClass = monsterType.AnimationClass;
            Id = monsterType.Id;
            AsciiCharacter = monsterType.AsciiCharacter;
            AsciiColour = monsterType.AsciiColour;

            // Note, can't block light due to the way moveables are treated in Map
        }

        public Monster(Tuple<int, int> coord, AIComponent aIComponent, MonsterType monsterType) :
            this(coord.Item1, coord.Item2, aIComponent, monsterType) 
        { }

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
