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
/// entirely - the gallery should stay quiet so the fence rows are the only thing to look at, with
/// one deliberate exception: a single goblin standing on the enclosure's west wall tile, for
/// manually checking that a blocked edge blocks combat as well as movement (see
/// dontrolle/BlazorRogue-internal#86 follow-up). Uses a fixed (not randomly-weighted) floor/wall
/// set so repeat visits render identically.
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

            // Every shape along the west/east sides of a real enclosure gets a non-blocking
            // companion tile one column outside it, completing its own single-line/dangling art
            // into a proper double-rail (see PlaceFenceEnclosure). Mirrors that here for each
            // shape's standalone swatch, reusing the row's existing 1-tile gaps so it doesn't
            // disturb the row's spacing.
            if (typeId == "fence_end_west")
            {
                PlaceFence(swatchX - 1, swatchRowY, "fence_post_east");
            }
            else if (typeId == "fence_end_east")
            {
                PlaceFence(swatchX + 1, swatchRowY, "fence_post_west");
            }
            else if (typeId == "fence_wall_west")
            {
                PlaceFence(swatchX - 1, swatchRowY, "fence_wall_east_companion");
            }
            else if (typeId == "fence_wall_east")
            {
                PlaceFence(swatchX + 1, swatchRowY, "fence_wall_west_companion");
            }
            else if (typeId == "fence_corner_sw")
            {
                PlaceFence(swatchX - 1, swatchRowY, "fence_corner_sw_companion");
            }
            else if (typeId == "fence_corner_se")
            {
                PlaceFence(swatchX + 1, swatchRowY, "fence_corner_se_companion");
            }

            // fence_wall_east's own companion sits at its usual +1 gap, but fence_wall_east is
            // immediately followed by fence_corner_sw, whose own companion sits at swatchX-1 of
            // *its* tile - with the standard 2-tile spacing that's the same gap tile. Widen just
            // this one gap by 1 so the two companions don't collide.
            swatchX += typeId == "fence_wall_east" ? 3 : 2;
        }
        PlaceFencePillar(swatchX, swatchRowY);

        // The same 3x3 enclosure (north wall + gate, west/east walls, south wall) validated by the
        // composite mockups and the Stage 1 manual check - confirms the shapes still compose
        // correctly as the catalog grows. Now the same PlaceFenceEnclosure the real procedural
        // placement pass (MapGeneratorBase.AddFenceEnclosures) uses.
        const int enclosureX0 = 3;
        const int enclosureY0 = 8;
        PlaceFenceEnclosure(x0: enclosureX0, y0: enclosureY0, width: 3, height: 3, gateColumn: 1);

        // A goblin (simple_ai - it will path toward and attack the player, including fleeing
        // through the gate rather than sitting still) standing on the east wall tile itself. That
        // tile isn't Blocking for occupancy, only edge-blocked on its *outward* (east) side, so
        // this is a legitimate reachable position, not a synthetic one - the same situation a
        // wandering monster can end up in during real play. On the east side (close to the
        // player's arrival point above) rather than the west, so there's less time for it to path
        // out through the gate before you reach it. Walk to the companion tile immediately outside
        // it (enclosureX0 + 3, enclosureY0 + 1) and press the move-west key: the goblin should be
        // neither attackable nor able to attack back, even though it's directly adjacent, because
        // the fence's blocked edge sits between the two tiles (dontrolle/BlazorRogue-internal#86
        // follow-up). If it's already slipped out through the gate by the time you arrive, that's
        // the gate working as intended (fully walkable) - leave and re-enter the gallery (Ctrl+G
        // twice) for a fresh goblin and try approaching more directly.
        _ = AddMonsterAt(enclosureX0 + 2, enclosureY0 + 1, configuration.MonsterTypes["goblin"]);

        // Sight radius is only Map.PlayerSightRadius (6) - too small to see the whole gallery from
        // one spot regardless, but this puts the swatch row mostly in view on arrival, with the
        // enclosure a short walk south.
        return Tuple.Create(8, 6);
    }
}
