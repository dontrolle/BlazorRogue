using System;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue.World.Generation;

/// <summary>
/// Deterministic dev-only level showing every current fence_* decoration type (see
/// dontrolle/BlazorRogue-internal#86) - a swatch row (one tile per shape, in decorations.json's
/// declaration order) plus the validated 3x3 rectangular enclosure, so a new fence addition can be
/// eyeballed for regressions without hand-splicing test placement code. Reached in a running game
/// via Ctrl+D (debug mode) then Ctrl+G (see Game.ToggleDebugLevelView, GamePage.razor) whenever
/// game-config.json's "debug_level" points at this level's number; never part of normal level
/// progression - see Data/levels.json's "fence_gallery" entry.
/// </summary>
/// <remarks>
/// Unlike <see cref="TestMapGenerator"/>, this overrides <see cref="GenerateMap"/> itself rather
/// than just <see cref="CreateLayout"/>, skipping doors/liquid pools/random decorations/monsters
/// entirely - the gallery should stay quiet so the fence rows are the only thing to look at. Uses a
/// fixed (not randomly-weighted) floor/wall set so repeat visits render identically.
/// </remarks>
class FenceGalleryMapGenerator(
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
    public const string Id = "fence_gallery_map_generator";

    readonly TileSet floorSet = game.Configuration.FloorSetById("grey");

    // Every single-tile fence shape, in decorations.json's declaration order. The freestanding
    // pillar (fence_pillar_west/east) is placed separately via PlaceFencePillar - a purely
    // decorative pair, unrelated to blocking or any actual fence line.
    static readonly string[] SwatchTypeIds =
    [
        "fence_straight",
        "fence_end_west",
        "fence_end_east",
        "fence_opening",
        "fence_wall_west",
        "fence_wall_east",
        "fence_corner_sw",
        "fence_corner_se",
        "fence_tjunction",
    ];

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

        const int swatchRowY = 3;
        int swatchX = 2;
        foreach (string typeId in SwatchTypeIds)
        {
            PlaceFence(swatchX, swatchRowY, typeId);
            swatchX += 2;
        }
        PlaceFencePillar(swatchX, swatchRowY);

        // The same 3x3 enclosure (north wall + gate, west/east walls, south wall) validated by the
        // composite mockups and the Stage 1 manual check - confirms the shapes still compose
        // correctly as the catalog grows. Now the same PlaceFenceEnclosure the real procedural
        // placement pass (MapGeneratorBase.AddFenceEnclosures) uses.
        PlaceFenceEnclosure(x0: 3, y0: 8, width: 3, height: 3, gateColumn: 1);

        // Sight radius is only Map.PlayerSightRadius (6) - too small to see the whole gallery from
        // one spot regardless, but this puts the swatch row mostly in view on arrival, with the
        // enclosure a short walk south.
        return Tuple.Create(8, 6);
    }
}
