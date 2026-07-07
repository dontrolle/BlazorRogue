using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorRogue.Entities
{
  public class MoveableType(
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
    public string AnimationClass { get; } = animationClass;
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string AsciiCharacter { get; } = asciiCharacter;
    public string AsciiColour { get; } = asciiColour;
    public int WeaponSkill { get; } = weaponSkill;
    public int WeaponDamage { get; } = weaponDamage;
    public int Toughness { get; } = toughness;
    public int Armour { get; } = armour;
    public int Wounds { get; } = wounds;
  }
}
