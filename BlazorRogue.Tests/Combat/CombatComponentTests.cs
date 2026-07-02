using BlazorRogue.Combat.Warhammer;
using BlazorRogue.Entities;

namespace BlazorRogue.Tests.Combat
{
  public class CombatComponentTests
  {
    private static Moveable CreateMoveable(int weaponSkill = 30, int weaponDamage = 8, int toughness = 30, int armour = 2, int wounds = 10)
    {
      var type = new MoveableType(
          id: "test",
          name: "Test Dummy",
          animationClass: "animated_test",
          asciiCharacter: "t",
          asciiColour: "white",
          weaponSkill: weaponSkill,
          weaponDamage: weaponDamage,
          toughness: toughness,
          armour: armour,
          wounds: wounds);

      return new Moveable(0, 0, aIComponent: null, type);
    }

    [Fact]
    public void ToughnessBonus_IsToughnessDividedByTen()
    {
      var moveable = CreateMoveable(toughness: 35);
      Assert.Equal(3, moveable.CombatComponent!.ToughnessBonus);
    }

    [Fact]
    public void ApplyDamage_ReducesWoundsByDamageMinusToughnessBonusAndArmour()
    {
      // toughness 30 => bonus 3, armour 2 soaks 5 of the 8 damage, leaving 3 wounds lost
      var moveable = CreateMoveable(toughness: 30, armour: 2, wounds: 10);
      moveable.CombatComponent!.ApplyDamage(8);

      Assert.Equal(7, moveable.CombatComponent.Wounds);
    }

    [Fact]
    public void ApplyDamage_BelowSoakThreshold_IncreasesWounds()
    {
      // NOTE: documents current (possibly unintended) behavior - when soaked damage (toughness
      // bonus + armour) exceeds the raw damage, wounds actually go up rather than being floored at 0.
      var moveable = CreateMoveable(toughness: 30, armour: 2, wounds: 10);
      moveable.CombatComponent!.ApplyDamage(1);

      Assert.Equal(14, moveable.CombatComponent.Wounds);
    }

    [Fact]
    public void ApplyDamage_KillsOwnerWhenWoundsReachZero()
    {
      var moveable = CreateMoveable(toughness: 0, armour: 0, wounds: 5);
      var killed = false;
      moveable.GameObjectKilled += (_, _) => killed = true;

      moveable.CombatComponent!.ApplyDamage(5);

      Assert.True(killed);
      Assert.True(moveable.CombatComponent.Wounds <= 0);
    }

    [Fact]
    public void GainAdvantage_IsCappedAtEight()
    {
      var moveable = CreateMoveable();
      moveable.CombatComponent!.GainAdvantage(20);

      Assert.Equal(8, moveable.CombatComponent.Advantage);
    }

    [Fact]
    public void GainAdvantage_Accumulates()
    {
      var moveable = CreateMoveable();
      moveable.CombatComponent!.GainAdvantage();
      moveable.CombatComponent.GainAdvantage(2);

      Assert.Equal(3, moveable.CombatComponent.Advantage);
    }

    [Fact]
    public void ResetAdvantage_SetsAdvantageToZero()
    {
      var moveable = CreateMoveable();
      moveable.CombatComponent!.GainAdvantage(4);
      moveable.CombatComponent.ResetAdvantage();

      Assert.Equal(0, moveable.CombatComponent.Advantage);
    }

    [Fact]
    public void LooseAdvantage_DecrementsByOne()
    {
      var moveable = CreateMoveable();
      moveable.CombatComponent!.GainAdvantage(4);
      moveable.CombatComponent.LooseAdvantage();

      Assert.Equal(3, moveable.CombatComponent.Advantage);
    }
  }
}
