using System;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue.World.Generation;

/// <summary>
/// Deterministic dev-only level showing every current fence_* decoration type (see
/// dontrolle/BlazorRogue-internal#86) - a swatch row (one tile per shape, in decorations.json's
/// declaration order) plus the validated 3x3 rectangular enclosure, so a new fence addition can be
/// eyeballed for regressions without hand-splicing test placement code. Reached in a running game
/// via Ctrl+D (debug mode) then Ctrl+G (see GamePage.razor); never part of normal level
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
        // correctly as the catalog grows.
        const int enclosureX = 3;
        const int enclosureY = 8;
        PlaceFence(enclosureX, enclosureY, "fence_end_west");
        PlaceFence(enclosureX + 1, enclosureY, "fence_opening");
        PlaceFence(enclosureX + 2, enclosureY, "fence_end_east");
        PlaceFence(enclosureX, enclosureY + 1, "fence_wall_west");
        PlaceFence(enclosureX + 2, enclosureY + 1, "fence_wall_east");
        PlaceFence(enclosureX, enclosureY + 2, "fence_corner_sw");
        PlaceFence(enclosureX + 1, enclosureY + 2, "fence_straight");
        PlaceFence(enclosureX + 2, enclosureY + 2, "fence_corner_se");

        // Sight radius is only Map.PlayerSightRadius (6) - too small to see the whole gallery from
        // one spot regardless, but this puts the swatch row mostly in view on arrival, with the
        // enclosure a short walk south.
        return Tuple.Create(8, 6);
    }

    void PlaceFence(int x, int y, string typeId) =>
        map.AddGameObject(
            new StaticDecorativeObject(x, y, configuration.StaticDecorativeObjectTypes[typeId])
        );
}
