using BlazorRogue.Combat.Warhammer;
using BlazorRogue.Entities;

namespace BlazorRogue.Tests.Combat
{
  public class FightingSystemTests
  {
    private static Moveable CreateMoveable(string id, int weaponSkill, int weaponDamage = 8, int toughness = 100, int armour = 0, int wounds = 1000)
    {
      var type = new MoveableType(
          id: id,
          name: id,
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
    public void CloseCombatAttack_ThrowsForNullAttacker()
    {
      var fightingSystem = new FightingSystem(game: null!);
      var defender = CreateMoveable("defender", 30);

      Assert.Throws<ArgumentNullException>(() => fightingSystem.CloseCombatAttack(null!, defender.CombatComponent!));
    }

    [Fact]
    public void CloseCombatAttack_ThrowsForNullDefender()
    {
      var fightingSystem = new FightingSystem(game: null!);
      var attacker = CreateMoveable("attacker", 30);

      Assert.Throws<ArgumentNullException>(() => fightingSystem.CloseCombatAttack(attacker.CombatComponent!, null!));
    }

    [Fact]
    public void CloseCombatAttack_HigherWeaponSkillWinsMoreOftenOverManyRounds()
    {
      // High toughness/wounds so nobody actually dies mid-way through the sample, which would
      // otherwise stop generating attacks against that combatant (a fresh dummy is used per round instead).
      var fightingSystem = new FightingSystem(game: null!);

      const int rounds = 2000;
      int strongAttackerHits = 0;
      int weakAttackerHits = 0;

      for (int i = 0; i < rounds; i++)
      {
        var strongAttacker = CreateMoveable("strong", weaponSkill: 70);
        var weakDefender = CreateMoveable("weakDefender", weaponSkill: 20);
        if (fightingSystem.CloseCombatAttack(strongAttacker.CombatComponent!, weakDefender.CombatComponent!))
        {
          strongAttackerHits++;
        }

        var weakAttacker = CreateMoveable("weak", weaponSkill: 20);
        var strongDefender = CreateMoveable("strongDefender", weaponSkill: 70);
        if (fightingSystem.CloseCombatAttack(weakAttacker.CombatComponent!, strongDefender.CombatComponent!))
        {
          weakAttackerHits++;
        }
      }

      Assert.True(
          strongAttackerHits > weakAttackerHits,
          $"Expected an attacker with much higher weapon skill to land more hits ({strongAttackerHits}) than one with much lower weapon skill ({weakAttackerHits}) over {rounds} rounds.");
    }
  }
}
