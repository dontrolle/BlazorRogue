using System.Globalization;
using System.Text;
using BlazorRogue.Entities;
using BlazorRogue.World;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation;

/// <summary>
/// Covers reproducible level generation: the same game seed generates the same dungeon, whatever
/// order its levels are visited in, while a different seed generates a different one.
/// </summary>
public class SeededGenerationTests
{
    // Levels whose generators actually use randomness. Fence Gallery is a fixed showcase, so a
    // different seed is not expected to change it.
    static readonly string[] RandomisedGeneratorIds =
    [
        BasicDungeonGenerator.Id,
        BSPGeneratorId,
        CaveGenerator.Id,
        TestMapGenerator.Id,
    ];
    const string BSPGeneratorId = "bsp_map_generator";

    static Map Generate(Game game, LevelConfiguration level) =>
        MapGeneratorFactory.Create(level, game).GenerateMap();

    // Everything about a generated map a player could tell apart: terrain, every object and
    // creature, and the rendered decoration sprites (which cover the wall-face/stair art picks).
    static string Fingerprint(Map map)
    {
        var sb = new StringBuilder();
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var tile = map.Tiles[x, y];
                _ = sb.Append(
                    CultureInfo.InvariantCulture,
                    $"t{x},{y}:{tile.TileSet.Id}/{tile.TileIndex}/{tile.Blocking}/{tile.Liquid?.Id}/{tile.Trap?.Type.Id};"
                );
                foreach (var d in map.Decorations[x, y])
                {
                    _ = sb.Append(
                        CultureInfo.InvariantCulture,
                        $"d{x},{y}:{d.ImageName}/{d.AnimationClass}/{d.VerticalOffset};"
                    );
                }
            }
        }
        foreach (var go in map.GameObjects)
        {
            _ = sb.Append(
                CultureInfo.InvariantCulture,
                $"g{go.GetType().Name}:{go.Name}@{go.X},{go.Y};"
            );
        }
        foreach (var m in map.Moveables)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"m{m.Name}@{m.X},{m.Y};");
        }
        return sb.ToString();
    }

    static IEnumerable<LevelConfiguration> RandomisedLevels(Game game) =>
        game.Configuration.Levels.Values.Where(l =>
            RandomisedGeneratorIds.Contains(l.MapGeneratorId)
        );

    [Fact]
    public void TheSameSeedGeneratesIdenticalLevels()
    {
        var first = new Game(seed: 1234);
        var second = new Game(seed: 1234);

        var levels = first.Configuration.Levels.Values.ToList();
        Assert.NotEmpty(levels);
        Assert.All(
            levels,
            level =>
                Assert.Equal(
                    Fingerprint(Generate(first, level)),
                    Fingerprint(Generate(second, level))
                )
        );
    }

    [Fact]
    public void RegeneratingALevelOnTheSameGameGivesTheSameLevel()
    {
        var game = new Game(seed: 99);

        Assert.All(
            game.Configuration.Levels.Values,
            level =>
                Assert.Equal(Fingerprint(Generate(game, level)), Fingerprint(Generate(game, level)))
        );
    }

    [Fact]
    public void ALevelDoesNotDependOnWhichLevelsWereGeneratedBeforeIt()
    {
        var levels = new Game(seed: 7).Configuration.Levels.Values.ToList();
        var forwards = new Game(seed: 7);
        var backwards = new Game(seed: 7);

        var forwardPrints = levels.ToDictionary(
            l => l.Number,
            l => Fingerprint(Generate(forwards, l))
        );
        var backwardPrints = Enumerable
            .Reverse(levels)
            .ToDictionary(l => l.Number, l => Fingerprint(Generate(backwards, l)));

        Assert.All(levels, l => Assert.Equal(forwardPrints[l.Number], backwardPrints[l.Number]));
    }

    [Fact]
    public void DifferentSeedsGenerateDifferentLevels()
    {
        var a = new Game(seed: 1);
        var b = new Game(seed: 2);

        var levels = RandomisedLevels(a).ToList();
        Assert.NotEmpty(levels);
        Assert.All(
            levels,
            level =>
                Assert.NotEqual(Fingerprint(Generate(a, level)), Fingerprint(Generate(b, level)))
        );
    }

    [Fact]
    public void TraplessAndTrappedLevelsAreBothReproducible()
    {
        // Traps are placed late in generation, so a seed that reproduces them proves the whole
        // pipeline up to that point is reproducible too.
        var settings = new SettingsMap(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object> { ["percentage_chance_of_traps"] = 0.2 }
                ),
            }
        );
        var level = new LevelConfiguration(
            number: 0,
            id: "seeded-trap-level",
            name: "Seeded Trap Level",
            height: 40,
            width: 40,
            generatorId: TestMapGenerator.Id,
            backgroundSoundtrack: "test.mp3",
            settingsMap: settings
        );

        string first = Fingerprint(Generate(new Game(seed: 5), level));
        string second = Fingerprint(Generate(new Game(seed: 5), level));

        Assert.Contains("/spike_trap;", first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void AGameExposesTheSeedItWasGiven() => Assert.Equal(31337, new Game(seed: 31337).Seed);

    [Fact]
    public void AnUnseededGamePicksANonNegativeSeedItCanBeReproducedFrom()
    {
        var game = new Game();
        Assert.True(game.Seed >= 0);

        var replay = new Game(seed: game.Seed);
        var level = game.Configuration.Levels[game.CurrentLevelNumber];
        Assert.Equal(Fingerprint(game.Map), Fingerprint(replay.Map));
        Assert.Equal(game.CurrentLevelNumber, level.Number);
    }

    [Fact]
    public void LevelSeedsAreStableAcrossRunsAndDifferPerLevelAndStream()
    {
        // Golden values: LevelSeed must not depend on anything randomized per process (e.g.
        // HashCode.Combine), or a seed from a bug report would not reproduce the same dungeon.
        Assert.Equal(
            LevelSeed.For(42, 3, LevelSeed.Generation),
            LevelSeed.For(42, 3, LevelSeed.Generation)
        );
        Assert.Equal(GoldenSeed, LevelSeed.For(42, 3, LevelSeed.Generation));

        Assert.NotEqual(
            LevelSeed.For(42, 3, LevelSeed.Generation),
            LevelSeed.For(42, 4, LevelSeed.Generation)
        );
        Assert.NotEqual(
            LevelSeed.For(42, 3, LevelSeed.Generation),
            LevelSeed.For(43, 3, LevelSeed.Generation)
        );
        Assert.NotEqual(
            LevelSeed.For(42, 3, LevelSeed.Generation),
            LevelSeed.For(42, 3, LevelSeed.Setup)
        );
        Assert.True(LevelSeed.For(-1, -1000, LevelSeed.Cosmetic) >= 0);
    }

    const int GoldenSeed = 1772147160;
}
