using BlazorRogue.AI;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue.Tests.Combat;

public class CombatComponentTests
{
    static Moveable CreateMoveable(
        int weaponSkill = 30,
        int weaponDamage = 8,
        int toughness = 30,
        int armour = 2,
        int wounds = 10
    )
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
            wounds: wounds,
            aiComponentId: AIComponentFactory.DefaultId,
            aiComponentSettings: SettingsMap.Empty,
            singular: true
        );

        return new Moveable(0, 0, aIComponent: null, type);
    }

    [Fact]
    public void ToughnessBonusIsToughnessDividedByTen()
    {
        var moveable = CreateMoveable(toughness: 35);
        Assert.Equal(3, moveable.CombatComponent!.ToughnessBonus);
    }

    [Fact]
    public void ApplyDamageReducesWoundsByDamageMinusToughnessBonusAndArmour()
    {
        // toughness 30 => bonus 3, armour 2 soaks 5 of the 8 damage, leaving 3 wounds lost
        var moveable = CreateMoveable(toughness: 30, armour: 2, wounds: 10);
        moveable.CombatComponent!.ApplyDamage(8);

        Assert.Equal(7, moveable.CombatComponent.Wounds);
    }

    [Fact]
    public void ApplyDamageBelowSoakThresholdDoesNotIncreaseWoundsBeyondMaxWounds()
    {
        // When soaked damage (toughness bonus + armour) exceeds the raw damage, wounds
        // should not go up. In other words, MaxWounds (aka initial wounds) should be respected.
        var moveable = CreateMoveable(toughness: 30, armour: 2, wounds: 10);
        moveable.CombatComponent!.ApplyDamage(1);

        Assert.Equal(10, moveable.CombatComponent.Wounds);
    }

    [Fact]
    public void ApplyDamageKillsOwnerWhenWoundsReachZero()
    {
        // GameObject.Kill() reaches References.Game directly, so it must be set up here rather
        // than relying on some other test's `new Game()` having run first in the same process -
        // that ordering isn't guaranteed and was the source of an intermittent NullReferenceException.
        _ = new Game();

        var moveable = CreateMoveable(toughness: 0, armour: 0, wounds: 5);
        bool killed = false;
        moveable.GameObjectKilled += (_, _) => killed = true;

        moveable.CombatComponent!.ApplyDamage(5);

        Assert.True(killed);
        Assert.True(moveable.CombatComponent.Wounds <= 0);
    }

#pragma warning disable xUnit1004 // Test methods should not be skipped
    [Fact(Skip = "Advantage disabled for now")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
    public void GainAdvantageIsCappedAtEight()
    {
        var moveable = CreateMoveable();
        moveable.CombatComponent!.GainAdvantage(20);

        Assert.Equal(8, moveable.CombatComponent.Advantage);
    }

#pragma warning disable xUnit1004 // Test methods should not be skipped
    [Fact(Skip = "Advantage disabled for now")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
    public void GainAdvantageAccumulates()
    {
        var moveable = CreateMoveable();
        moveable.CombatComponent!.GainAdvantage();
        moveable.CombatComponent.GainAdvantage(2);

        Assert.Equal(3, moveable.CombatComponent.Advantage);
    }

#pragma warning disable xUnit1004 // Test methods should not be skipped
    [Fact(Skip = "Advantage disabled for now")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
    public void ResetAdvantageSetsAdvantageToZero()
    {
        var moveable = CreateMoveable();
        moveable.CombatComponent!.GainAdvantage(4);
        moveable.CombatComponent.ResetAdvantage();

        Assert.Equal(0, moveable.CombatComponent.Advantage);
    }

#pragma warning disable xUnit1004 // Test methods should not be skipped
    [Fact(Skip = "Advantage disabled for now")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
    public void LooseAdvantageDecrementsByOne()
    {
        var moveable = CreateMoveable();
        moveable.CombatComponent!.GainAdvantage(4);
        moveable.CombatComponent.LooseAdvantage();

        Assert.Equal(3, moveable.CombatComponent.Advantage);
    }
}
