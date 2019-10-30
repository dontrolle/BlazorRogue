using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorRogue.Entities
{
    public class MoveableType
    {
        public MoveableType(
            string id, 
            string name, 
            string animationClass, 
            string asciiCharacter, 
            string asciiColour, 
            int weaponSkill, 
            int weaponDamage, 
            int toughness,
            int armour,
            int wounds)
        {
            Id = id;
            Name = name;
            AnimationClass = animationClass;
            AsciiCharacter = asciiCharacter;
            AsciiColour = asciiColour;
            WeaponSkill = weaponSkill;
            WeaponDamage = weaponDamage;
            Toughness = toughness;
            Armour = armour;
            Wounds = wounds;
        }

        public string AnimationClass { get; }
        public string Id { get; }
        public string Name { get; }
        public string AsciiCharacter { get; }
        public string AsciiColour { get; }
        public int WeaponSkill { get; }
        public int WeaponDamage { get; }
        public int Toughness { get; }
        public int Armour { get; }
        public int Wounds { get; }
    }
}
