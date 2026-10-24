using System;
using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue.World.Generation;

/// <summary>
/// Deterministic dev-only level showing every configured <see cref="TrapType"/> twice - once
/// <see cref="TrapState.Hidden"/> and once <see cref="TrapState.Revealed"/> - so trap drawing,
/// triggering and detection can be checked in the browser without waiting for a random level to
/// roll a trap. Reached in a running game via Ctrl+D (debug mode) then Ctrl+G whenever
/// game-config.json's "debug_level" points at this level's number; never part of normal level
/// progression - see Data/levels.json's "trap_gallery" entry.
/// </summary>
/// <remarks>
/// Layout: one row per trap type, in trapsets.json's declaration order, on every second line
/// starting at <see cref="FirstRowY"/>. Within a row the hidden trap is at x = 6 and the revealed
/// one at x = 9; after <see cref="RowsPerBlock"/> types a new block starts
/// <see cref="BlockWidth"/> columns further east. The player arrives at
/// (<see cref="PlayerX"/>, <see cref="FirstRowY"/>), so walking east along the first row hits the
/// first type's hidden trap after 3 steps. Overrides <see cref="GenerateMap"/> itself and so places
/// no doors, monsters, liquids or random decorations: the hero can walk the whole gallery.
/// </remarks>
class TrapGalleryMapGenerator(
    int width,
    int height,
    int levelNumber,
    Game game,
    SettingsMap settings
)
    : MapGeneratorBase(
        width,
        height,
        levelNumber,
        game,
        game.Configuration.WallSetById("dungeon"),
        settings
    )
{
    public const string Id = "trap_gallery_map_generator";

    public const int PlayerX = 3;
    public const int FirstRowY = 2;
    public const int RowsPerBlock = 8;
    public const int BlockWidth = 12;
    const int HiddenX = 6;
    const int RevealedX = 9;

    readonly TileSet floorSet = game.Configuration.FloorSetById("grey");

    /// <summary>Where the hidden trap of the trap type at <paramref name="index"/> is placed.</summary>
    public static (int X, int Y) HiddenPosition(int index) => Position(index, HiddenX);

    /// <summary>Where the revealed trap of the trap type at <paramref name="index"/> is placed.</summary>
    public static (int X, int Y) RevealedPosition(int index) => Position(index, RevealedX);

    static (int X, int Y) Position(int index, int x) =>
        (x + (BlockWidth * (index / RowsPerBlock)), FirstRowY + (2 * (index % RowsPerBlock)));

    public override Map GenerateMap(Moveable? existingPlayer = null)
    {
        var playerPos = CreateLayout();
        AddPlayer(playerPos, existingPlayer);
        map.PostGenInitalize();
        return map;
    }

    protected override Tuple<int, int> CreateLayout()
    {
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                bool isBorder = x == 0 || x == map.Width - 1 || y == 0 || y == map.Height - 1;
                if (isBorder)
                {
                    PlaceWall(x, y);
                }
                else
                {
                    PlaceFloor(x, y, floorSet);
                }
            }
        }

        foreach (var (type, index) in configuration.TrapTypes.Select((t, i) => (t, i)))
        {
            var (hiddenX, hiddenY) = HiddenPosition(index);
            var (revealedX, revealedY) = RevealedPosition(index);
            map.Tiles[hiddenX, hiddenY].Trap = new Trap(type) { State = TrapState.Hidden };
            map.Tiles[revealedX, revealedY].Trap = new Trap(type) { State = TrapState.Revealed };
        }

        return Tuple.Create(PlayerX, FirstRowY);
    }
}
