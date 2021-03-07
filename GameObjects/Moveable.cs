using System;
using BlazorRogue.AI;
using BlazorRogue.Combat.Warhammer;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue
{
    public class Moveable : GameObject
    {
        public string AnimationClass { get; protected set; }
        public string Id { get; }
        public string AsciiCharacter { get; }
        public string AsciiColour { get; }

        public override string? InfoText => Name;

        public Moveable(int x, int y, AIComponent? aIComponent, MoveableType monsterType) : 
            base(x, y, monsterType.Name, aIComponent, new CombatComponent(monsterType.WeaponSkill, monsterType.WeaponDamage, monsterType.Toughness, monsterType.Armour, monsterType.Wounds))
        {
            InvisibleOutsideFov = true;
            Blocking = true;
            AnimationClass = monsterType.AnimationClass;
            Id = monsterType.Id;
            AsciiCharacter = monsterType.AsciiCharacter;
            AsciiColour = monsterType.AsciiColour;

            // Note, can't block light due to the way Moveables are treated in Map
        }

        public Moveable(Tuple<int, int> coord, AIComponent? aIComponent, MoveableType monsterType) :
            this(coord.Item1, coord.Item2, aIComponent, monsterType) 
        { }

        public override void Render(Map map)
        {
            if (AnimationClass == null)
            {
                throw new InvalidOperationException("AnimationClass not set.");
            }

            map.MoveableDecorations[x, y].Add(new Decoration(this, null) { AnimationClass = AnimationClass, Character = AsciiCharacter, CharacterColor = AsciiColour });
        }

        public override void Move(int xDelta, int yDelta)
        {
            base.Move(xDelta, yDelta);
            Game.SoundManager.PlayWalkSound();
        }
    }
}
