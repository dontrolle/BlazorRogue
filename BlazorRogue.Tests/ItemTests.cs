using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue.Tests;

public class ItemTests
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

    [Fact]
    public void ItemNeverBlocksMovementOrLight()
    {
        var item = new Item(0, 0, HealthPotion);

        Assert.False(item.Blocking);
        Assert.False(item.BlocksLight);
    }

    [Fact]
    public void RenderAddsADecorationWithTheItemTypesArtAndCharacter()
    {
        var game = new Game();
        var (x, y) = (game.Map.Player.X, game.Map.Player.Y);
        var item = new Item(x, y, HealthPotion);
        game.Map.AddGameObject(item);

        item.Render(game.Map);

        var decoration = Assert.Single(game.Map.Decorations[x, y], d => d.GameObject == item);
        Assert.Equal(HealthPotion.Image, decoration.ImageName);
        Assert.Equal(HealthPotion.ImgFolder, decoration.ImageFolder);
        Assert.Equal(HealthPotion.Character, decoration.Character);
        Assert.Equal(HealthPotion.CharacterColor, decoration.CharacterColor);
    }
}
