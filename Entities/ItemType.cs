namespace BlazorRogue.Entities;

/// <summary>
/// Whether an item is consumed on use or toggled on/off as worn equipment - see
/// <see cref="ItemType.Kind"/>.
/// </summary>
enum ItemKind
{
    /// <summary>Applies its effect once, then is removed from the inventory (or its stack shrinks by one).</summary>
    UseOnce,

    /// <summary>Toggled equipped/unequipped; its effect applies only while equipped.</summary>
    Equipable,
}

/// <summary>
/// What an item's effect does - see <see cref="ItemType.EffectKind"/> and
/// <see cref="ItemType.EffectMagnitude"/>.
/// </summary>
enum ItemEffectKind
{
    /// <summary>Restores <see cref="ItemType.EffectMagnitude"/> hitpoints. <see cref="ItemKind.UseOnce"/> only.</summary>
    Heal,

    /// <summary>Adds <see cref="ItemType.EffectMagnitude"/> armour while equipped. <see cref="ItemKind.Equipable"/> only.</summary>
    ArmourBonus,
}

/// <summary>
/// A kind of pickup-able item, parsed from <c>Data/items.json</c>. Carried on the floor by an
/// item game object and, once picked up, referenced from an inventory entry.
/// </summary>
sealed class ItemType(
    string id,
    string name,
    ItemKind kind,
    string imgFolder,
    string image,
    string character,
    string characterColor,
    ItemEffectKind effectKind,
    int effectMagnitude
)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public ItemKind Kind { get; } = kind;
    public string ImgFolder { get; } = imgFolder;
    public string Image { get; } = image;
    public string Character { get; } = character;
    public string CharacterColor { get; } = characterColor;
    public ItemEffectKind EffectKind { get; } = effectKind;
    public int EffectMagnitude { get; } = effectMagnitude;
}
