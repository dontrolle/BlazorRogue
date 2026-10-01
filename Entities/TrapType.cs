using System.Collections.Generic;

namespace BlazorRogue.Entities;

/// <summary>
/// What happens to whoever triggers a trap of a given <see cref="TrapType"/>.
/// </summary>
enum TrapEffectKind
{
    /// <summary><see cref="TrapType.EffectMagnitude"/> damage to whoever triggers it.</summary>
    Damage,
}

/// <summary>
/// A kind of floor trap (spike trap, ...), parsed from <c>Data/trapsets.json</c>. Mirrors
/// <see cref="LiquidType"/>'s <c>{kind, magnitude}</c> effect shape.
/// </summary>
sealed class TrapType(
    string id,
    string name,
    IReadOnlyList<string> images,
    string character,
    string characterColor,
    string infoText,
    TrapEffectKind effectKind,
    int effectMagnitude,
    bool startsHidden,
    bool reusable
)
{
    public string Id { get; } = id;

    /// <summary>Lower-case display noun used in messages ("spike trap").</summary>
    public string Name { get; } = name;

    /// <summary>
    /// Atlas sprite names a revealed trap of this type is drawn with - one is picked per placed
    /// trap, so a type can have visual variants (e.g. the four rune sprites).
    /// </summary>
    public IReadOnlyList<string> Images { get; } = images;

    /// <summary>The glyph a revealed trap renders as in the ASCII renderer.</summary>
    public string Character { get; } = character;

    public string CharacterColor { get; } = characterColor;

    /// <summary>Tooltip text for a revealed trap of this type.</summary>
    public string InfoText { get; } = infoText;

    public TrapEffectKind EffectKind { get; } = effectKind;
    public int EffectMagnitude { get; } = effectMagnitude;

    /// <summary>
    /// Whether a placed trap of this type starts hidden, i.e. undrawn until stepped on or detected.
    /// </summary>
    public bool StartsHidden { get; } = startsHidden;

    /// <summary>Whether the trap stays armed after triggering, rather than being spent.</summary>
    public bool Reusable { get; } = reusable;
}
