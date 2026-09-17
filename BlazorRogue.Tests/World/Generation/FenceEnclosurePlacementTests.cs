using System;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.World;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation;

/// <summary>
/// Covers MapGeneratorBase.AddFenceEnclosures/PlaceFenceEnclosure (see
/// dontrolle/BlazorRogue-internal#86) - the procedural placement pass that fences off a random
/// rectangle of open floor, structured like AddLiquidPools (opt-in via a common.fence_enclosures
/// settings block, sampled-origin retry loop, skipped entirely when unconfigured).
/// </summary>
public class FenceEnclosurePlacementTests
{
    static LevelConfiguration LevelWithSettings(
        SettingsMap settingsMap,
        int width = 30,
        int height = 30
    ) =>
        new(
            number: 0,
            id: "fence-enclosure-placement-test-level",
            name: "Fence Enclosure Placement Test Level",
            height: height,
            width: width,
            generatorId: TestMapGenerator.Id,
            backgroundSoundtrack: "test.mp3",
            settingsMap: settingsMap
        );

    // Zeroes every other percentage-chance decoration so a fence enclosure's footprint is never
    // coincidentally blocked by unrelated content - same technique as
    // StatuePlacementTests.SettingsWithStatueChance.
    static SettingsMap SettingsWithFenceEnclosures(
        int countMin,
        int countMax,
        int widthMin = 3,
        int widthMax = 3,
        int heightMin = 3,
        int heightMax = 3
    ) =>
        new(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object>
                    {
                        ["fence_enclosures"] = new SettingsMap(
                            new Dictionary<string, object>
                            {
                                ["count_min"] = countMin,
                                ["count_max"] = countMax,
                                ["width_min"] = widthMin,
                                ["width_max"] = widthMax,
                                ["height_min"] = heightMin,
                                ["height_max"] = heightMax,
                            }
                        ),
                        ["percentage_chance_of_statues"] = 0.0,
                        ["percentage_chance_of_fountains"] = 0.0,
                        ["percentage_chance_of_bones"] = 0.0,
                        ["percentage_chance_of_tables"] = 0.0,
                        ["percentage_chance_of_altars"] = 0.0,
                        ["percentage_chance_of_barrels"] = 0.0,
                        ["percentage_chance_of_graveyard_clutter"] = 0.0,
                        ["percentage_chance_of_runes"] = 0.0,
                        ["percentage_chance_of_leaves"] = 0.0,
                        ["percentage_chance_of_dust"] = 0.0,
                        ["percentage_chance_of_lilypad"] = 0.0,
                        ["percentage_chance_of_puddle_large"] = 0.0,
                        ["percentage_chance_of_spider_web_in_corner"] = 0.0,
                        ["percentage_chance_of_torch"] = 0.0,
                        ["percentage_chance_of_chests"] = 0.0,
                        ["percentage_chance_of_items"] = 0.0,
                        ["percentage_chance_of_door"] = 0.0,
                    }
                ),
            }
        );

    static List<string?> RenderedFenceImages(Map map)
    {
        var images = new List<string?>();
        map.ForEachTile(
            (x, y) =>
            {
                foreach (var decoration in map.Decorations[x, y])
                {
                    if ((decoration.ImageName ?? "").StartsWith("fence_", StringComparison.Ordinal))
                    {
                        images.Add(decoration.ImageName);
                    }
                }
            }
        );
        return images;
    }

    [Fact]
    public void AGuaranteed3x3EnclosureHasExactlyTheExpectedTilesOfEachShape()
    {
        var game = new Game();
        var level = LevelWithSettings(SettingsWithFenceEnclosures(countMin: 1, countMax: 1));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();
        var images = RenderedFenceImages(map);

        Assert.Equal(1, images.Count(i => i == "fence_2")); // end_west
        Assert.Equal(1, images.Count(i => i == "fence_4")); // end_east
        Assert.Equal(1, images.Count(i => i == "fence_3")); // opening/gate
        Assert.Equal(1, images.Count(i => i == "fence_1")); // end_west's fence_post_east companion
        Assert.Equal(1, images.Count(i => i == "fence_5")); // end_east's fence_post_west companion
        // wall_west (height 3 => one row) + its wall_east_companion, one tile outside the enclosure
        Assert.Equal(2, images.Count(i => i == "fence_7"));
        // wall_east + its wall_west_companion, one tile outside the enclosure
        Assert.Equal(2, images.Count(i => i == "fence_6"));
        Assert.Equal(1, images.Count(i => i == "fence_12")); // corner_sw
        Assert.Equal(1, images.Count(i => i == "fence_14")); // corner_se
        Assert.Equal(1, images.Count(i => i == "fence_13")); // straight (south wall middle)
        Assert.Equal(1, images.Count(i => i == "fence_11")); // corner_sw's companion
        Assert.Equal(1, images.Count(i => i == "fence_15")); // corner_se's companion
        Assert.Equal(14, images.Count);
    }

    [Fact]
    public void NoFenceEnclosureSpawnsWhenCountMaxIsZero()
    {
        var game = new Game();
        var level = LevelWithSettings(SettingsWithFenceEnclosures(countMin: 0, countMax: 0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.Empty(RenderedFenceImages(map));
    }

    [Fact]
    public void NoFenceEnclosureSpawnsWhenTheSettingsBlockIsAbsent()
    {
        // Absent common.fence_enclosures entirely (not just a zeroed one) - the real opt-in default
        // every existing level.json entry relies on.
        var game = new Game();
        var level = LevelWithSettings(SettingsMap.Empty);

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.Empty(RenderedFenceImages(map));
    }

    // A wall tile bleeds its own decoration onto whichever floor tile is adjacent to it
    // (Tile.RenderHalfWall onto the tile north of a wall, RenderShadow onto the tile south of one -
    // both a VerticalOffset trick, universal to every wall in the game). The fence enclosure's
    // north/south rows use tall-picket "Infront" art that isn't designed to coexist with that -
    // see IsFenceEnclosureEligible. This map's interior is only 4 rows tall (height 6, border walls
    // at y=0/5), so *every* possible y0 for a 3-tall enclosure has its north or south row directly
    // against a border wall - the fix should reject all of them, placing nothing.
    [Fact]
    public void NoFenceEnclosurePlacesItsWallRowsDirectlyAgainstARealWall()
    {
        var game = new Game();
        var level = LevelWithSettings(
            SettingsWithFenceEnclosures(countMin: 1, countMax: 1),
            width: 10,
            height: 6
        );

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.Empty(RenderedFenceImages(map));
    }

    // Same idea as NoFenceEnclosurePlacesItsWallRowsDirectlyAgainstARealWall, but one row taller
    // (height 7, interior rows y=1..5) - exactly one y0 (2) keeps both wall rows clear of the
    // border, so the fix must still succeed when a valid slot actually exists, not just correctly
    // refuse when one doesn't. Wide (not just tall) so TestMapGenerator's own two incidental random
    // freestanding walls (CreateLayout, unrelated to fences) can't plausibly block every one of the
    // many valid x0 columns along that one valid row by chance - a narrower map made this flaky.
    [Fact]
    public void AFenceEnclosureStillPlacesWhenAValidSlotExists()
    {
        var game = new Game();
        var level = LevelWithSettings(
            SettingsWithFenceEnclosures(countMin: 1, countMax: 1),
            width: 40,
            height: 7
        );

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.NotEmpty(RenderedFenceImages(map));
    }

    [Fact]
    public void VariableSizedEnclosuresGenerateRepeatedlyWithoutThrowingOrOverlapping()
    {
        // A generous count/size range, run several times - nothing should throw, and every placed
        // fence tile's underlying tile must have been open floor (AddFenceEnclosures' own
        // eligibility check already guarantees no two enclosures - or an enclosure and anything
        // else - share a tile; this is a regression guard on that guarantee holding as the fence
        // catalog or placement logic changes).
        var level = LevelWithSettings(
            SettingsWithFenceEnclosures(
                countMin: 1,
                countMax: 3,
                widthMin: 3,
                widthMax: 6,
                heightMin: 3,
                heightMax: 6
            )
        );

        for (int i = 0; i < 5; i++)
        {
            var game = new Game();
            var map = MapGeneratorFactory.Create(level, game).GenerateMap();

            var fenceCoords = new List<(int X, int Y)>();
            map.ForEachTile(
                (x, y) =>
                {
                    int fenceCount = map.Decorations[x, y]
                        .Count(d =>
                            (d.ImageName ?? "").StartsWith("fence_", StringComparison.Ordinal)
                        );
                    Assert.True(fenceCount <= 1, $"Overlapping fence decorations at ({x},{y}).");
                    if (fenceCount == 1)
                    {
                        fenceCoords.Add((x, y));
                    }
                }
            );

            Assert.NotEmpty(fenceCoords);
        }
    }
}
