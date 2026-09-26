using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation;

/// <summary>
/// Covers MapGeneratorBase's fountain placement and Fountain's own rendering: like Statue, the
/// fountain's own tile isn't Blocking (the pool art comfortably fits a moveable standing on/in
/// front of it), but OccupiesTile keeps other solid props off it. Its wall-tile overlay
/// (VerticalOffset -1, see Fountain.Render) needs no Edge/BlockedEdges fencing, unlike Statue's,
/// because it lands on a Wall tile - already unconditionally impassable regardless of any
/// GameObject sitting on it.
/// </summary>
public class FountainPlacementTests
{
    static LevelConfiguration LevelWithSettings(SettingsMap settingsMap) =>
        new(
            number: 0,
            id: "fountain-placement-test-level",
            name: "Fountain Placement Test Level",
            height: 30,
            width: 30,
            generatorId: TestMapGenerator.Id,
            backgroundSoundtrack: "test.mp3",
            settingsMap: settingsMap
        );

    // Zeroes every other percentage-chance decoration so fountain placement can be asserted
    // without another, unrelated decoration coincidentally landing on the fountain's tile or the
    // wall tile above it.
    static SettingsMap SettingsWithFountainChance(double chance) =>
        new(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object>
                    {
                        ["percentage_chance_of_fountains"] = chance,
                        ["percentage_chance_of_statues"] = 0.0,
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
    public void FountainsSitOnAWalkableFloorTileAgainstAWallAndRenderPoolPlusBothAnimatedOverlays()
    {
        var game = new Game();
        var level = LevelWithSettings(SettingsWithFountainChance(1.0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        var fountains = map.GameObjects.OfType<Fountain>().ToList();
        Assert.NotEmpty(fountains);

        Assert.All(
            fountains,
            fountain =>
            {
                // The art fits a moveable standing on/in front of the fountain - its own tile
                // isn't Blocking (checking the tile's own structural Blocking rather than the live
                // map.IsBlocked, since AddMonsters runs after decorations and may legitimately
                // place a monster to stand there afterwards).
                Assert.False(fountain.Blocking);
                Assert.False(map.Tiles[fountain.X, fountain.Y].Blocking);

                Assert.Equal(TileType.Wall, map.Tiles[fountain.X, fountain.Y - 1].TileType);

                Assert.Contains(
                    map.Decorations[fountain.X, fountain.Y],
                    d =>
                        d.ImageName == "fountain_pool"
                        && d.VerticalOffset == 0
                        && d.DecorationLayer == Decoration.Layer.Behind
                );
                Assert.Contains(
                    map.Decorations[fountain.X, fountain.Y],
                    d => d.AnimationClass == "animated_fountain_floor" && d.VerticalOffset == 0
                );
                Assert.Contains(
                    map.Decorations[fountain.X, fountain.Y],
                    d => d.AnimationClass == "animated_fountain_wall" && d.VerticalOffset == -1
                );
            }
        );
    }

    [Fact]
    public void NoFountainsSpawnWhenTheirChanceIsZero()
    {
        var game = new Game();
        var level = LevelWithSettings(SettingsWithFountainChance(0.0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.DoesNotContain(map.GameObjects, go => go is Fountain);
    }

    // Fountain isn't Blocking, so - unlike every other solid prop, which already excludes its
    // siblings via Blocking - it needs GameObject.OccupiesTile to keep e.g. a coffin from landing
    // on the same tile as its basin (see MapGeneratorBase.TileOccupied). Only fountains and
    // graveyard_clutter (which includes "coffin") are enabled here, both at 100%:
    // graveyard_clutter is checked right after fountains for each tile (see
    // AddRandomPostGenFloorDecorationsAt), so this reproduces the exact ordering that matters -
    // pairing fountain with a prop placed *before* it in that method (e.g. barrels) would instead
    // just starve fountains of every eligible tile before they get a turn.
    static SettingsMap SettingsWithFountainsAndGraveyardClutterChance(double chance) =>
        new(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object>
                    {
                        ["percentage_chance_of_fountains"] = chance,
                        ["percentage_chance_of_graveyard_clutter"] = chance,
                        ["percentage_chance_of_statues"] = 0.0,
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
    public void NoSolidPropEverSharesATileWithAFountain()
    {
        var game = new Game();
        var level = LevelWithSettings(SettingsWithFountainsAndGraveyardClutterChance(1.0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        var fountains = map.GameObjects.OfType<Fountain>().ToList();
        Assert.NotEmpty(fountains);

        Assert.All(
            fountains,
            fountain =>
                Assert.DoesNotContain(
                    map.GameObjectByCoord[fountain.X, fountain.Y],
                    go => go.OccupiesTile && go is not Fountain
                )
        );
    }
}
