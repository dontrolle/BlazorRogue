using BlazorRogue.AI;
using BlazorRogue.Components;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue.Tests.Components;

public class InventoryComponentTests
{
    static readonly ItemType HealthPotion = new(
        id: "health_potion",
        name: "Health potion",
        kind: ItemKind.UseOnce,
        imgFolder: "uf_items",
        image: "potion_red",
        character: "!",
        characterColor: "red",
        effectKind: ItemEffectKind.Heal,
        effectMagnitude: 20
    );

    static readonly ItemType RingOfProtection = new(
        id: "ring_of_protection",
        name: "Ring of protection",
        kind: ItemKind.Equipable,
        imgFolder: "uf_items",
        image: "ring_silver",
        character: "=",
        characterColor: "silver",
        effectKind: ItemEffectKind.ArmourBonus,
        effectMagnitude: 1
    );

    // A CombatComponent-bearing owner is required - InventoryComponent.Use() applies use_once/
    // equipable effects straight onto Owner.CombatComponent (see ApplyEffect).
    static Moveable CreateOwner(int wounds = 50)
    {
        var type = new MoveableType(
            id: "test",
            name: "Test Dummy",
            animationClass: "animated_test",
            asciiCharacter: "t",
            asciiColour: "white",
            weaponSkill: 30,
            weaponDamage: 8,
            toughness: 30,
            armour: 2,
            wounds: wounds,
            aiComponentId: AIComponentFactory.DefaultId,
            aiComponentSettings: SettingsMap.Empty,
            singular: true
        );

        return new Moveable(
            0,
            0,
            aIComponent: null,
            type,
            inventoryComponent: new InventoryComponent()
        );
    }

    [Fact]
    public void TryPickUpAssignsTheFirstFreeLetter()
    {
        var owner = CreateOwner();

        Assert.True(owner.InventoryComponent!.TryPickUp(HealthPotion, out char letter));
        Assert.Equal('a', letter);
    }

    [Fact]
    public void TryPickUpStacksASecondUseOnceItemOntoTheSameLetter()
    {
        var owner = CreateOwner();
        owner.InventoryComponent!.TryPickUp(HealthPotion, out char firstLetter);

        Assert.True(owner.InventoryComponent.TryPickUp(HealthPotion, out char secondLetter));

        Assert.Equal(firstLetter, secondLetter);
        Assert.Equal(2, owner.InventoryComponent.Items[firstLetter].Count);
    }

    [Fact]
    public void TryPickUpGivesASecondEquipableItemItsOwnLetter()
    {
        var owner = CreateOwner();
        owner.InventoryComponent!.TryPickUp(RingOfProtection, out char firstLetter);

        Assert.True(owner.InventoryComponent.TryPickUp(RingOfProtection, out char secondLetter));

        Assert.NotEqual(firstLetter, secondLetter);
        Assert.Equal(2, owner.InventoryComponent.Items.Count);
    }

    [Fact]
    public void TryPickUpReusesALetterFreedByAnEarlierDrop()
    {
        var owner = CreateOwner();
        owner.InventoryComponent!.TryPickUp(HealthPotion, out char firstLetter); // 'a'
        owner.InventoryComponent.TryPickUp(RingOfProtection, out char secondLetter); // 'b'
        owner.InventoryComponent.Remove(firstLetter); // frees 'a'

        Assert.True(owner.InventoryComponent.TryPickUp(RingOfProtection, out char reusedLetter));

        Assert.Equal(firstLetter, reusedLetter);
        Assert.NotEqual(secondLetter, reusedLetter);
    }

    [Fact]
    public void TryPickUpFailsOnceAllTwentySixLettersAreTaken()
    {
        var owner = CreateOwner();
        for (int i = 0; i < 26; i++)
        {
            Assert.True(owner.InventoryComponent!.TryPickUp(RingOfProtection, out _));
        }

        bool pickedUp = owner.InventoryComponent!.TryPickUp(RingOfProtection, out char letter);

        Assert.False(pickedUp);
        Assert.Equal(default, letter);
        Assert.Equal(26, owner.InventoryComponent.Items.Count);
    }

    [Fact]
    public void UseOnAStackDecrementsAndFreesTheLetterAtZero()
    {
        var owner = CreateOwner();
        owner.InventoryComponent!.TryPickUp(HealthPotion, out char letter);
        owner.InventoryComponent.TryPickUp(HealthPotion, out _); // Count == 2

        owner.InventoryComponent.Use(letter);
        Assert.True(owner.InventoryComponent.Items.ContainsKey(letter));
        Assert.Equal(1, owner.InventoryComponent.Items[letter].Count);

        owner.InventoryComponent.Use(letter);
        Assert.False(owner.InventoryComponent.Items.ContainsKey(letter));
    }

    [Fact]
    public void UseOnAHealthPotionHealsTheOwner()
    {
        var owner = CreateOwner(wounds: 50);
        owner.CombatComponent!.ApplyDamage(15); // DamageSoak 5 -> 10 lost -> 40
        owner.InventoryComponent!.TryPickUp(HealthPotion, out char letter);

        owner.InventoryComponent.Use(letter); // heals 20 -> 60, clamped at MaxWounds (50)

        Assert.Equal(50, owner.CombatComponent.Wounds);
    }

    [Fact]
    public void UseOnAnEquipableItemTogglesEquippedAndAppliesTheArmourBonus()
    {
        var owner = CreateOwner();
        owner.InventoryComponent!.TryPickUp(RingOfProtection, out char letter);
        int soakBeforeEquip = owner.CombatComponent!.DamageSoak;

        owner.InventoryComponent.Use(letter);
        Assert.True(owner.InventoryComponent.Items[letter].IsEquipped);
        Assert.Equal(soakBeforeEquip + 1, owner.CombatComponent.DamageSoak);

        owner.InventoryComponent.Use(letter); // toggles back off
        Assert.False(owner.InventoryComponent.Items[letter].IsEquipped);
        Assert.Equal(soakBeforeEquip, owner.CombatComponent.DamageSoak);
    }

    [Fact]
    public void EquippingTwoCopiesOfTheSameRingStacksTheirArmourBonusesAdditively()
    {
        var owner = CreateOwner();
        owner.InventoryComponent!.TryPickUp(RingOfProtection, out char firstLetter);
        owner.InventoryComponent.TryPickUp(RingOfProtection, out char secondLetter);
        int soakBeforeEquip = owner.CombatComponent!.DamageSoak;

        owner.InventoryComponent.Use(firstLetter);
        owner.InventoryComponent.Use(secondLetter);

        Assert.Equal(soakBeforeEquip + 2, owner.CombatComponent.DamageSoak);
    }

    [Fact]
    public void RemoveUnequipsAnEquippedItemBeforeDroppingIt()
    {
        var owner = CreateOwner();
        owner.InventoryComponent!.TryPickUp(RingOfProtection, out char letter);
        int soakBeforeEquip = owner.CombatComponent!.DamageSoak;
        owner.InventoryComponent.Use(letter); // equip

        var dropped = owner.InventoryComponent.Remove(letter);

        Assert.Equal(RingOfProtection, dropped);
        Assert.Equal(soakBeforeEquip, owner.CombatComponent.DamageSoak);
        Assert.False(owner.InventoryComponent.Items.ContainsKey(letter));
    }

    [Fact]
    public void UseOnAnUnknownLetterReturnsNull()
    {
        var owner = CreateOwner();

        Assert.Null(owner.InventoryComponent!.Use('z'));
    }

    [Fact]
    public void RemoveOnAnUnknownLetterReturnsNull()
    {
        var owner = CreateOwner();

        Assert.Null(owner.InventoryComponent!.Remove('z'));
    }
}
