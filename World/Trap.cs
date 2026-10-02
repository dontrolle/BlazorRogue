using BlazorRogue.Entities;

namespace BlazorRogue.World;

/// <summary>Where a placed <see cref="Trap"/> is in its life cycle.</summary>
enum TrapState
{
    /// <summary>Armed but undrawn - the player doesn't know it is there.</summary>
    Hidden,

    /// <summary>Armed and drawn, either because it was detected or because its type never hides.</summary>
    Revealed,

    /// <summary>Triggered and, not being reusable, disarmed for good.</summary>
    Spent,
}

/// <summary>
/// A trap placed on a map tile (see <see cref="Tile.Trap"/>). Deliberately a plain per-tile state
/// holder rather than a GameObject: a trap never blocks, moves or is picked up, and keeping it off
/// the GameObject lists means a hidden one can't leak into either renderer by accident.
/// </summary>
sealed class Trap(TrapType type)
{
    public TrapType Type { get; } = type;

    public TrapState State { get; set; } =
        type.StartsHidden ? TrapState.Hidden : TrapState.Revealed;
}
