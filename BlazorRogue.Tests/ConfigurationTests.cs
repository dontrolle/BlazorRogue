using BlazorRogue.AI;
using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.Tests;

public class ConfigurationTests
{
    static Configuration ParseConfiguration()
    {
        var configuration = new Configuration();
        configuration.Parse();
        return configuration;
    }

    [Fact]
    public void ParseLoadsAllKnownMonsterTypes()
    {
        var configuration = ParseConfiguration();

        Assert.True(configuration.MonsterTypes.ContainsKey("skeleton"));
        Assert.True(configuration.MonsterTypes.ContainsKey("goblin"));
        Assert.True(configuration.MonsterTypes.ContainsKey("ogre"));
    }

    [Fact]
    public void ParseLoadsMonsterStatsWithSaneValues()
    {
        // Deliberately doesn't assert exact combat stat values from monsters.json - those get
        // rebalanced often and shouldn't make this test brittle. Instead this checks Configuration
        // maps every stat field into a sensible range for every monster.
        var configuration = ParseConfiguration();

        Assert.NotEmpty(configuration.MonsterTypes);
        Assert.All(
            configuration.MonsterTypes.Values,
            monster =>
            {
                Assert.False(string.IsNullOrWhiteSpace(monster.Name));
                Assert.True(monster.WeaponSkill > 0);
                Assert.True(monster.WeaponDamage > 0);
                Assert.True(monster.Toughness > 0);
                Assert.True(monster.Armour >= 0);
                Assert.True(monster.Wounds > 0);
            }
        );
    }

    [Fact]
    public void ParseDefaultsAIComponentIdWhenMonsterHasNoAiComponent()
    {
        var configuration = ParseConfiguration();

        Assert.Equal(AIComponentFactory.DefaultId, configuration.MonsterTypes["rat"].AIComponentId);
    }

    [Fact]
    public void ParseReadsExplicitAIComponentIdWhenMonsterHasOne()
    {
        var configuration = ParseConfiguration();

        Assert.Equal(
            RandomWalkAIComponent.ComponentId,
            configuration.MonsterTypes["flies"].AIComponentId
        );
    }

    [Fact]
    public void ParseValidatesEveryMonstersAIComponentIdIsKnown()
    {
        // Configuration.Parse() checks every monster's ai_component id against AIComponentFactory
        // immediately after loading monsters.json - an unknown id would have thrown inside
        // ParseConfiguration() above, before this line ever runs. Mirrors
        // ParseValidatesEveryLevelsGeneratorIdIsKnown for the map-generator-id guarantee.
        var configuration = ParseConfiguration();

        Assert.All(
            configuration.MonsterTypes.Values,
            monster => Assert.True(AIComponentFactory.IsKnown(monster.AIComponentId))
        );
    }

    [Fact]
    public void ParseLoadsHeroTypes()
    {
        var configuration = ParseConfiguration();

        Assert.True(configuration.HeroTypes.ContainsKey("templar"));
        Assert.Equal("Templar", configuration.HeroTypes["templar"].Name);
    }

    [Fact]
    public void ParseLoadsHeroesAndMonstersWithAnAnimatedPrefixedAnimationClass()
    {
        // AnimationCssGenerator (see Rendering/AnimationCssGenerator.cs) silently drops any
        // AnimationClass that doesn't start with "animated_" instead of failing - a typo there
        // doesn't break the build or throw, it just quietly leaves that hero/monster with no sprite
        // animation in-game. This locks in the prefix convention so a typo fails a test instead.
        var configuration = ParseConfiguration();

        Assert.All(
            configuration.HeroTypes.Values.Concat(configuration.MonsterTypes.Values),
            moveable => Assert.StartsWith("animated_", moveable.AnimationClass)
        );
    }

    [Fact]
    public void ParseLoadsFloorAndWallSets()
    {
        var configuration = ParseConfiguration();

        Assert.NotEmpty(configuration.StandardFloorSets);
        Assert.NotEmpty(configuration.DungeonWallSets);
    }

    [Fact]
    public void ParseLoadsWallSetEdgeIndexesWhenPresent()
    {
        // "cave" and "hedge" both define a top-level "edges" object in wallsets.json (used by
        // CaveGenerator's WallEdge decorations); wall-sets that don't define one (e.g. "crypt",
        // used by the dungeon generator, which never reads these) should parse as null rather
        // than throwing.
        var configuration = ParseConfiguration();

        var cave = configuration.WallSetById("cave");
        Assert.Equal((1, 2), cave.EdgeNorthIndexes);
        Assert.Equal((5, 6), cave.EdgeSouthIndexes);
        Assert.Equal((3, 4), cave.EdgeFreeIndexes);

        var hedge = configuration.WallSetById("hedge");
        Assert.Equal((1, 2), hedge.EdgeNorthIndexes);
        Assert.Equal((5, 6), hedge.EdgeSouthIndexes);
        Assert.Equal((3, 4), hedge.EdgeFreeIndexes);

        var crypt = configuration.WallSetById("crypt");
        Assert.Null(crypt.EdgeNorthIndexes);
        Assert.Null(crypt.EdgeSouthIndexes);
        Assert.Null(crypt.EdgeFreeIndexes);
    }

    [Fact]
    public void ParseLoadsOutdoorWallSets()
    {
        var configuration = ParseConfiguration();

        Assert.Contains(configuration.OutdoorWallSets, t => t.Id == "hedge");
    }

    [Fact]
    public void ParseLoadsStaticDecorativeObjectTypes()
    {
        var configuration = ParseConfiguration();

        Assert.NotEmpty(configuration.StaticDecorativeObjectTypes);
    }

    [Fact]
    public void ParseLoadsFloorSetStairImagesWhenPresent()
    {
        var configuration = ParseConfiguration();

        var grey = configuration.FloorSetById("grey");
        Assert.Equal((8, 9), grey.StairImageIndexes);

        var blue = configuration.FloorSetById("blue");
        Assert.Equal((8, 9), blue.StairImageIndexes);

        var dark = configuration.FloorSetById("dark");
        Assert.Equal((8, 9), dark.StairImageIndexes);
    }

    [Fact]
    public void ParseLoadsFloorSetsWithoutStairImagesAsNull()
    {
        // Special floorsets don't currently ship dedicated stair art; Stair.Render is expected to
        // fall back to Configuration.DefaultStairsFloorSet for these.
        var configuration = ParseConfiguration();

        var groundGrass = configuration.FloorSetById("ground_grass");
        Assert.Null(groundGrass.StairImageIndexes);
    }

    [Fact]
    public void ParseSetsDefaultStairsFloorSetToGrey()
    {
        // Configuration.Parse() throws if "grey" (the hardcoded fallback stair-image source)
        // doesn't itself define img_stairs, so this test also indirectly covers that guard - a
        // missing fallback would have made Parse() throw before we ever got here.
        var configuration = ParseConfiguration();

        Assert.Equal("grey", configuration.DefaultStairsFloorSet.Id);
        Assert.NotNull(configuration.DefaultStairsFloorSet.StairImageIndexes);
    }

    [Fact]
    public void ParseLoadedFloorSetIdsAreUnique()
    {
        // Configuration.Parse() reads from the real Data files, so we cover the duplicate-id guard
        // indirectly here by asserting all currently-loaded floor set ids are unique - a duplicate
        // would previously have caused Parse() itself to throw before we ever got here.
        var configuration = ParseConfiguration();
        var ids = configuration
            .StandardFloorSets.Select(t => t.Id)
            .Concat(configuration.SpecialFloorSets.Select(t => t.Id));

        Assert.Equal(ids.Count(), ids.Distinct().Count());
    }

    [Fact]
    public void ParseLoadsLevelsWithSaneData()
    {
        // Deliberately doesn't assert exact id/name/width/height values from levels.json - those
        // get tuned often and shouldn't make this test brittle. Instead this locks in the *shape*
        // of the data: every level's Number matches the dictionary key it's stored under, and its
        // id/name/dimensions are non-trivial.
        var configuration = ParseConfiguration();

        Assert.NotEmpty(configuration.Levels);
        Assert.All(
            configuration.Levels,
            pair =>
            {
                var (number, level) = pair;
                Assert.Equal(number, level.Number);
                Assert.False(string.IsNullOrWhiteSpace(level.Id));
                Assert.False(string.IsNullOrWhiteSpace(level.Name));
                Assert.False(string.IsNullOrWhiteSpace(level.BackgroundSoundtrack));
                Assert.True(level.Width > 0);
                Assert.True(level.Height > 0);
            }
        );
    }

    [Fact]
    public void ParseLoadedLevelIdsAreUnique()
    {
        // Level *numbers* are already guarded directly (Configuration.levels.TryAdd throws on a
        // repeat), but level *ids* (the string) aren't - mirrors ParseLoadedFloorSetIdsAreUnique
        // by covering that indirectly against the real data instead.
        var configuration = ParseConfiguration();
        var ids = configuration.Levels.Values.Select(l => l.Id);

        Assert.Equal(ids.Count(), ids.Distinct().Count());
    }

    [Fact]
    public void ParseValidatesEveryLevelsGeneratorIdIsKnown()
    {
        // Configuration.Parse() checks every level's generator id against MapGeneratorFactory
        // immediately after loading levels.json - an unknown id would have thrown inside
        // ParseConfiguration() above, before this line ever runs. This exercises that same check
        // again directly so the guarantee has a named, discoverable test rather than only being an
        // implicit side effect of every other test in this file succeeding.
        var configuration = ParseConfiguration();

        Assert.All(
            configuration.Levels.Values,
            level => Assert.True(MapGeneratorFactory.IsKnown(level.MapGeneratorId))
        );
    }

    [Fact]
    public void ParseValidatesEveryLevelsWallTileSetIdsAreKnown()
    {
        // Configuration.Parse() also checks every id referenced by a level's
        // "common.wall_tile_set" weights against the parsed wall-sets - an unknown id would have
        // thrown inside ParseConfiguration() above, before this line ever runs. This exercises
        // WallSetById() directly against every id currently in use, the same way
        // ParseValidatesEveryLevelsGeneratorIdIsKnown does for generator ids.
        var configuration = ParseConfiguration();

        Assert.All(
            configuration.Levels.Values,
            level =>
            {
                var weights = level
                    .SettingsMap.GetMap("common", SettingsMap.Empty)
                    .GetWeightedIds("wall_tile_set", []);
                Assert.All(weights, w => Assert.NotNull(configuration.WallSetById(w.Id)));
            }
        );
    }
}
