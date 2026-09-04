using BlazorRogue.Entities;

namespace BlazorRogue.Components;

/// <summary>
/// Marks a <see cref="GameObjects.Item"/> as pickup-able (the 'g' key) and carries the
/// <see cref="Entities.ItemType"/> that ends up in the picking-up <see cref="InventoryComponent"/>.
/// </summary>
class PickupableComponent(ItemType itemType) : Component
{
    public ItemType ItemType { get; } = itemType;
}
