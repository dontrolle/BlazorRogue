namespace BlazorRogue.World;

enum TileType
{
    Black,
    Wall,
    Floor,
    Ground,

    // A walkable pool of liquid (water/mud/acid/lava). Not blocking and not light-blocking - the
    // hazard behaviour rides on Tile.Liquid, not on this value. Generator feature-placement is all
    // opt-in on `== Floor`, so a Liquid tile is naturally excluded from doors/decorations/etc.
    Liquid,
}
