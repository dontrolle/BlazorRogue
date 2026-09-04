using BlazorRogue.Components;
using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

/// <summary>
/// An item sitting on the floor, waiting to be picked up (the 'g' key) into a
/// <see cref="InventoryComponent"/>. Never blocks movement or light.
/// </summary>
class Item(int x, int y, ItemType itemType)
    : GameObject(x, y, itemType.Name, pickupableComponent: new PickupableComponent(itemType))
{
    ItemType ItemType => PickupableComponent!.ItemType;

    public override string InfoText => ItemType.Name;

    public override void Render(Map map) =>
        map.Decorations[X, Y]
            .Add(
                new Decoration(this, ItemType.Image, ItemType.ImgFolder)
                {
                    Character = ItemType.Character,
                    CharacterColor = ItemType.CharacterColor,
                }
            );
}
