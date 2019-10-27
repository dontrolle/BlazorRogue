using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorRogue.Entities
{
    public class MonsterType
    {
        public MonsterType(string id, string name, CombatComponent cc, string animationClass, string asciiCharacter, string asciiColour)
        {
            Id = id;
            Name = name;
            CombatComponent = cc;
            AnimationClass = animationClass;
            AsciiCharacter = asciiCharacter;
            AsciiColour = asciiColour;
        }

        public string AnimationClass { get; }
        public string Id { get; }
        public string Name { get; }
        public CombatComponent CombatComponent { get; }
        public string AsciiCharacter { get; }
        public string AsciiColour { get; }

    }
}
