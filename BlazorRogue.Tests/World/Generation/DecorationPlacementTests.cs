using System.Collections.Generic;
using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.World;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation;

/// <summary>
/// Covers MapGeneratorBase.PlaceDustIfEligible: dust always comes as a pair - one piece on the
/// wall tile, one on the floor tile directly below it - and which of the three image pairs
/// (straight run / NW corner / NE corner) gets used depends on whether floor is open immediately
/// to the wall tile's west or east.
/// </summary>
public class DecorationPlacementTests
{
    // Mirrors Data/decorations.json's "dust" image tags.
    static readonly Dictionary<string, string> DustImages = new()
    {
        ["wall_straight"] = "dust_1",
        ["wall_ne"] = "dust_2",
        ["wall_nw"] = "dust_3",
        ["floor_straight"] = "dust_4",
        ["floor_ne"] = "dust_5",
        ["floor_nw"] = "dust_6",
    };

    static LevelConfiguration LevelWithSettings(SettingsMap settingsMap) =>
        new(
            number: 0,
            id: "decoration-placement-test-level",
            name: "Decoration Placement Test Level",
            height: 30,
            width: 30,
            generatorId: TestMapGenerator.Id,
            backgroundSoundtrack: "test.mp3",
            settingsMap: settingsMap
        );

    static SettingsMap SettingsWithDustChance(double chance) =>
        new(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object> { ["percentage_chance_of_dust"] = chance }
                ),
            }
        );

    [Fact]
    public void DustAlwaysComesInMatchedWallAndFloorPairsWithTheCorrectCornerImages()
    {
        var game = new Game();
        var level = LevelWithSettings(SettingsWithDustChance(1.0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        var wallDust = map.GameObjects.Where(go =>
            go.Name == "Dust" && map.Tiles[go.X, go.Y].TileType == TileType.Wall
        );

        Assert.NotEmpty(wallDust);

        Assert.All(
            wallDust,
            go =>
            {
                int x = go.X;
                int y = go.Y;

                // A wall tile only ever gets a dust piece when there's floor directly below it.
                Assert.Equal(TileType.Floor, map.Tiles[x, y + 1].TileType);

                // The paired floor piece must exist right below.
                Assert.Contains(
                    map.GameObjects,
                    other => other.Name == "Dust" && other.X == x && other.Y == y + 1
                );

                // The corner check reads one row down from the wall tile - beside the floor tile
                // below it, not beside the wall tile itself. A rectangular room's top wall is a
                // contiguous run, so the tile beside a top-wall tile is essentially always another
                // wall; the corner only becomes visible next to the room's perpendicular side wall,
                // one row down.
                bool sideWallToWest = x > 0 && map.Tiles[x - 1, y + 1].TileType == TileType.Wall;
                bool sideWallToEast =
                    x < map.Width - 1 && map.Tiles[x + 1, y + 1].TileType == TileType.Wall;

                var (wallTag, floorTag) =
                    sideWallToWest ? ("wall_nw", "floor_nw")
                    : sideWallToEast ? ("wall_ne", "floor_ne")
                    : ("wall_straight", "floor_straight");

                Assert.Contains(map.Decorations[x, y], d => d.ImageName == DustImages[wallTag]);
                Assert.Contains(
                    map.Decorations[x, y + 1],
                    d => d.ImageName == DustImages[floorTag]
                );
            }
        );

        // The all-floor test map's rectangular border wall guarantees a real NW corner post (its
        // west column) and a real NE corner post (its east column) - assert both actually got
        // exercised, so this test can't silently degrade back into only ever checking the straight
        // case (which is what happened with the original, same-row corner check this replaced).
        Assert.Contains(
            wallDust,
            go => map.Decorations[go.X, go.Y].Any(d => d.ImageName == "dust_3") // wall_nw
        );
        Assert.Contains(
            wallDust,
            go => map.Decorations[go.X, go.Y].Any(d => d.ImageName == "dust_2") // wall_ne
        );
    }

    [Fact]
    public void NoDustSpawnsWhenItsChanceIsZero()
    {
        var game = new Game();
        var level = LevelWithSettings(SettingsWithDustChance(0.0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.DoesNotContain(map.GameObjects, go => go.Name == "Dust");
    }
}
