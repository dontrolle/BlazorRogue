using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue.Tests;

// Map.PickUpItemsAtPlayer/UseInventoryItem/DropInventoryItem - the engine side of the 'g'/'i'+'u'/
// 'i'+'d' keys (GamePage.razor owns the key-handling/modal UI itself, which isn't unit-testable -
// see TESTING.md).
public class ItemInteractionTests
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

    static Item AddItemAtPlayer(Game game, ItemType itemType)
    {
        var item = new Item(game.Map.Player.X, game.Map.Player.Y, itemType);
        game.Map.AddGameObject(item);
        return item;
    }

    // Map generation can drop items, chests and decorations on any floor tile, the player's own
    // start tile included, which makes assertions about exactly what is on that tile flaky. Park
    // the player on a tile we know is empty first.
    static void PlacePlayerOnEmptyTile(Game game)
    {
        var map = game.Map;
        for (int x = 1; x < map.Width - 1; x++)
        {
            for (int y = 1; y < map.Height - 1; y++)
            {
                if (
                    map.Tiles[x, y].TileType == TileType.Floor
                    && !map.IsBlocked(x, y)
                    && !map.GameObjectByCoord[x, y].Any()
                )
                {
                    map.Player.PlaceAt(x, y);
                    return;
                }
            }
        }

        throw new InvalidOperationException("No empty floor tile found to place the player on.");
    }

    [Fact]
    public void PickUpItemsAtPlayerPicksUpAnItemAndRemovesItFromTheFloor()
    {
        var game = new Game();
        PlacePlayerOnEmptyTile(game);
        var item = AddItemAtPlayer(game, HealthPotion);

        bool pickedUp = game.Map.PickUpItemsAtPlayer();

        Assert.True(pickedUp);
        Assert.DoesNotContain(item, game.Map.GameObjects);
        Assert.Contains(
            game.Map.Player.InventoryComponent!.Items.Values,
            entry => entry.ItemType == HealthPotion
        );
        Assert.Contains("You pick up a Health potion.", game.Messages);
    }

    [Fact]
    public void PickUpItemsAtPlayerReturnsFalseOnAnEmptyTile()
    {
        var game = new Game();
        PlacePlayerOnEmptyTile(game);

        Assert.False(game.Map.PickUpItemsAtPlayer());
    }

    [Fact]
    public void PickUpItemsAtPlayerPicksUpEveryItemOnTheTileInOnePress()
    {
        var game = new Game();
        PlacePlayerOnEmptyTile(game);
        AddItemAtPlayer(game, HealthPotion);
        AddItemAtPlayer(game, RingOfProtection);

        bool pickedUp = game.Map.PickUpItemsAtPlayer();

        Assert.True(pickedUp);
        Assert.Equal(2, game.Map.Player.InventoryComponent!.Items.Count);
    }

    [Fact]
    public void PickUpItemsAtPlayerLeavesTheItemOnTheFloorWhenInventoryIsFull()
    {
        var game = new Game();
        for (int i = 0; i < 26; i++)
        {
            Assert.True(game.Map.Player.InventoryComponent!.TryPickUp(RingOfProtection, out _));
        }
        var item = AddItemAtPlayer(game, HealthPotion);

        bool pickedUp = game.Map.PickUpItemsAtPlayer();

        Assert.False(pickedUp);
        Assert.Contains(item, game.Map.GameObjects);
        Assert.Contains("Your inventory is full.", game.Messages);
    }

    [Fact]
    public void UseInventoryItemHealsTheOwnerAndAddsAMessage()
    {
        var game = new Game();
        game.Map.Player.CombatComponent!.ApplyDamage(1); // some headroom below max, without dying
        int woundsBeforeHeal = game.Map.Player.CombatComponent.Wounds;
        game.Map.Player.InventoryComponent!.TryPickUp(HealthPotion, out char letter);

        bool used = game.Map.UseInventoryItem(letter);

        Assert.True(used);
        Assert.True(game.Map.Player.CombatComponent.Wounds >= woundsBeforeHeal);
        Assert.Contains("You drink the Health potion and recover 20 hitpoints.", game.Messages);
    }

    [Fact]
    public void UseInventoryItemTogglesEquipAndAddsAMessage()
    {
        var game = new Game();
        int soakBeforeEquip = game.Map.Player.CombatComponent!.DamageSoak;
        game.Map.Player.InventoryComponent!.TryPickUp(RingOfProtection, out char letter);

        Assert.True(game.Map.UseInventoryItem(letter));
        Assert.Equal(soakBeforeEquip + 1, game.Map.Player.CombatComponent.DamageSoak);
        Assert.Contains("You put on the Ring of protection.", game.Messages);

        Assert.True(game.Map.UseInventoryItem(letter));
        Assert.Equal(soakBeforeEquip, game.Map.Player.CombatComponent.DamageSoak);
        Assert.Contains("You take off the Ring of protection.", game.Messages);
    }

    [Fact]
    public void UseInventoryItemOnAnUnknownLetterAddsANoSuchItemMessageAndReturnsFalse()
    {
        var game = new Game();

        Assert.False(game.Map.UseInventoryItem('z'));
        Assert.Contains("No such item.", game.Messages);
    }

    [Fact]
    public void DropInventoryItemPlacesTheItemBackOnTheFloorAndRemovesItFromInventory()
    {
        var game = new Game();
        var player = game.Map.Player;
        player.InventoryComponent!.TryPickUp(HealthPotion, out char letter);

        bool dropped = game.Map.DropInventoryItem(letter);

        Assert.True(dropped);
        Assert.False(player.InventoryComponent.Items.ContainsKey(letter));
        Assert.Contains(
            game.Map.GameObjects.OfType<Item>(),
            item => item.X == player.X && item.Y == player.Y
        );
        Assert.Contains("You drop the Health potion.", game.Messages);
    }

    [Fact]
    public void DroppingAnEquippedItemUnequipsItFirst()
    {
        var game = new Game();
        int soakBeforeEquip = game.Map.Player.CombatComponent!.DamageSoak;
        game.Map.Player.InventoryComponent!.TryPickUp(RingOfProtection, out char letter);
        game.Map.UseInventoryItem(letter); // equip

        game.Map.DropInventoryItem(letter);

        Assert.Equal(soakBeforeEquip, game.Map.Player.CombatComponent.DamageSoak);
        Assert.Contains("You take off the Ring of protection.", game.Messages);
        Assert.Contains("You drop the Ring of protection.", game.Messages);
    }

    [Fact]
    public void DropInventoryItemOnAnUnknownLetterAddsANoSuchItemMessageAndReturnsFalse()
    {
        var game = new Game();

        Assert.False(game.Map.DropInventoryItem('z'));
        Assert.Contains("No such item.", game.Messages);
    }
}
