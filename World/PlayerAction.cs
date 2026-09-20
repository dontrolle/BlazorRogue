namespace BlazorRogue.World;

/// <summary>
/// A player's intended action for one turn - the input to <see cref="Map.TakeTurn"/>. Named
/// PlayerAction rather than Action to avoid colliding with <see cref="System.Action"/>, which
/// several other types in this namespace (e.g. <see cref="Decoration.OnUse"/>) already use.
///
/// <see cref="Move"/> covers both "step in this direction" and "attack whatever's standing
/// there" - moving into an occupied tile already attacks automatically (see
/// <see cref="Map.HandlePlayerMove"/>), so there's no separate Attack case.
///
/// <see cref="UseDirection"/> matches <see cref="Map.HandlePlayerUse"/>'s shift+direction "use
/// whatever's in that direction" (open a door, pull a lever, ...) - including
/// <see cref="Direction.None"/> for "use whatever's on my own tile" (e.g. descending stairs).
/// This is a distinct case from <see cref="UseItem"/>, which is the inventory window's 'u'
/// command (use/equip a carried item by letter) and has nothing to do with direction.
/// </summary>
abstract record PlayerAction
{
    PlayerAction() { }

    internal sealed record Move(Direction Direction) : PlayerAction;

    internal sealed record UseDirection(Direction Direction) : PlayerAction;

    internal sealed record UseItem(char Letter) : PlayerAction;

    internal sealed record DropItem(char Letter) : PlayerAction;

    internal sealed record PickUp : PlayerAction;
}
