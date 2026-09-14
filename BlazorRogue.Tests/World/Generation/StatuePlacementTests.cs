using System.Collections.Generic;
using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation;

/// <summary>
/// Covers MapGeneratorBase's statue placement and Statue's two-part rendering: the statue's own
/// tile isn't Blocking (a moveable can stand on/in front of it), but it acts as a fence (see
/// Edge/GameObject.BlockedEdges) across the edge shared with the tile above - where its "top" half
/// is drawn (VerticalOffset -1 on the same GameObject, see Statue.Render) - so a moveable can't
/// step directly between the statue's tile and the tile north of it, though every other direction
/// (including standing right behind it) is unaffected.
/// </summary>
public class StatuePlacementTests
{
    static LevelConfiguration LevelWithSettings(SettingsMap settingsMap) =>
        new(
            number: 0,
            id: "statue-placement-test-level",
            name: "Statue Placement Test Level",
            height: 30,
            width: 30,
            generatorId: TestMapGenerator.Id,
            backgroundSoundtrack: "test.mp3",
            settingsMap: settingsMap
        );

    // Zeroes every other percentage-chance decoration so statue placement/blocking can be asserted
    // without another, unrelated decoration coincidentally landing on the tile above a statue.
    static SettingsMap SettingsWithStatueChance(double chance) =>
        new(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object>
                    {
                        ["percentage_chance_of_statues"] = chance,
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

    [Fact]
    public void TheStatuesOwnTileIsWalkableButCrossingToTheTileAboveIsFenced()
    {
        var game = new Game();
        var level = LevelWithSettings(SettingsWithStatueChance(1.0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        var statues = map.GameObjects.OfType<Statue>().ToList();
        Assert.NotEmpty(statues);

        Assert.All(
            statues,
            statue =>
            {
                // The art fits a moveable standing on/in front of the statue - its own tile isn't
                // Blocking (checking the tile's own structural Blocking rather than the live
                // map.IsBlocked, since AddMonsters runs after decorations and may legitimately
                // place a monster to stand there afterwards).
                Assert.False(statue.Blocking);
                Assert.False(map.Tiles[statue.X, statue.Y].Blocking);
                Assert.False(map.Tiles[statue.X, statue.Y - 1].Blocking);

                // Can't step directly between the statue's tile and the tile its "top" half bleeds
                // onto, in either direction...
                Assert.True(
                    map.IsMovementBlockedAcrossEdge(statue.X, statue.Y, statue.X, statue.Y - 1)
                );
                Assert.True(
                    map.IsMovementBlockedAcrossEdge(statue.X, statue.Y - 1, statue.X, statue.Y)
                );

                // ...but every other direction out of the statue's own tile is unaffected - you can
                // walk right up to/around it.
                Assert.False(
                    map.IsMovementBlockedAcrossEdge(statue.X, statue.Y, statue.X, statue.Y + 1)
                );
                Assert.False(
                    map.IsMovementBlockedAcrossEdge(statue.X, statue.Y, statue.X - 1, statue.Y)
                );
                Assert.False(
                    map.IsMovementBlockedAcrossEdge(statue.X, statue.Y, statue.X + 1, statue.Y)
                );

                Assert.Contains(
                    map.Decorations[statue.X, statue.Y],
                    d => d.ImageName == "statue_bottom" && d.VerticalOffset == 0
                );
                Assert.Contains(
                    map.Decorations[statue.X, statue.Y],
                    d => d.ImageName == "statue_top" && d.VerticalOffset == -1
                );
            }
        );
    }

    [Fact]
    public void NoStatuesSpawnWhenTheirChanceIsZero()
    {
        var game = new Game();
        var level = LevelWithSettings(SettingsWithStatueChance(0.0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.DoesNotContain(map.GameObjects, go => go is Statue);
    }

    // Statue isn't Blocking, so - unlike every other solid prop, which already excludes its
    // siblings via Blocking - it needs GameObject.OccupiesTile to keep e.g. a coffin from landing
    // on the same tile as its base (see MapGeneratorBase.TileOccupied). Only statues and
    // graveyard_clutter (which includes "coffin") are enabled here, both at 100%: graveyard_clutter
    // is checked after statues for each tile (see AddRandomPostGenFloorDecorationsAt), so this
    // reproduces the exact ordering the original bug report hit.
    static SettingsMap SettingsWithStatuesAndGraveyardClutterChance(double chance) =>
        new(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object>
                    {
                        ["percentage_chance_of_statues"] = chance,
                        ["percentage_chance_of_graveyard_clutter"] = chance,
                        ["percentage_chance_of_bones"] = 0.0,
                        ["percentage_chance_of_tables"] = 0.0,
                        ["percentage_chance_of_altars"] = 0.0,
                        ["percentage_chance_of_barrels"] = 0.0,
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

    [Fact]
    public void NoSolidPropEverSharesATileWithAStatue()
    {
        var game = new Game();
        var level = LevelWithSettings(SettingsWithStatuesAndGraveyardClutterChance(1.0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        var statues = map.GameObjects.OfType<Statue>().ToList();
        Assert.NotEmpty(statues);

        Assert.All(
            statues,
            statue =>
                Assert.DoesNotContain(
                    map.GameObjectByCoord[statue.X, statue.Y],
                    go => go.OccupiesTile && go is not Statue
                )
        );
    }
}
