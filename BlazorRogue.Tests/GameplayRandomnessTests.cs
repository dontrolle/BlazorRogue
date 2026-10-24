using System.Globalization;
using System.Text;
using BlazorRogue.Entities;
using BlazorRogue.Tests.TestSupport;

namespace BlazorRogue.Tests;

/// <summary>
/// Gameplay randomness (combat dice, monster movement, healing) comes from the game's own seeded
/// stream: the same seed and actions replay identically, and games don't touch each other's dice.
/// </summary>
public class GameplayRandomnessTests
{
    const int Turns = 250;

    // Everything about the world a player could see change: where everything stands, and its wounds.
    static string Snapshot(HeadlessPlayDriver driver)
    {
        var sb = new StringBuilder();
        var player = driver.Map.Player;
        _ = sb.Append(
            CultureInfo.InvariantCulture,
            $"P@{player.X},{player.Y}:{player.CombatComponent?.Wounds};"
        );
        foreach (var m in driver.Map.Moveables)
        {
            _ = sb.Append(
                CultureInfo.InvariantCulture,
                $"{m.Name}@{m.X},{m.Y}:{m.CombatComponent?.Wounds};"
            );
        }
        _ = sb.Append(string.Join('|', driver.Game.Messages));
        return sb.ToString();
    }

    sealed class Run
    {
        readonly HeadlessPlayDriver driver;
        readonly RandomHazardAvoidingPolicy policy;

        public List<string> Transcript { get; } = [];

        public Run(int seed, int? gameplaySeed = null)
        {
            var configuration = new Configuration();
            configuration.Parse();
            configuration.ClearTrapTypes();
            driver = new HeadlessPlayDriver(new Game(configuration, seed, gameplaySeed));
            policy = new RandomHazardAvoidingPolicy(new Random(seed));
        }

        public bool Step()
        {
            if (driver.Map.IsGameOver)
            {
                return false;
            }
            // What GameSession.Activate does before serving each request: References.Game is
            // process-wide, and this run shares the process with the other game.
            References.Game = driver.Game;
            _ = driver.TakeTurn(policy);
            Transcript.Add(Snapshot(driver));
            return true;
        }

        public Run Play(int turns)
        {
            for (int i = 0; i < turns && Step(); i++) { }
            return this;
        }
    }

    [Fact]
    public void TheSameSeedAndActionsReplayTurnByTurn() =>
        SeededRuns.Each(
            8,
            seed =>
                Assert.Equal(
                    new Run(seed).Play(Turns).Transcript,
                    new Run(seed).Play(Turns).Transcript
                )
        );

    [Fact]
    public void TheGameplaySeedDefaultsToOneDerivedFromTheGameSeed()
    {
        Assert.Equal(new Game(seed: 5).GameplaySeed, new Game(seed: 5).GameplaySeed);
        Assert.NotEqual(new Game(seed: 5).GameplaySeed, new Game(seed: 6).GameplaySeed);
        Assert.Equal(42, new Game(seed: 5, gameplaySeed: 42).GameplaySeed);
    }

    [Fact]
    public void ADifferentGameplaySeedChangesTheOutcomes()
    {
        bool anyDiffered = false;
        SeededRuns.Each(
            8,
            seed =>
            {
                var a = new Run(seed, gameplaySeed: 1).Play(Turns);
                var b = new Run(seed, gameplaySeed: 2).Play(Turns);
                anyDiffered |= !a.Transcript.SequenceEqual(b.Transcript);
            }
        );
        Assert.True(anyDiffered);
    }

    [Fact]
    public void GamesRunningInterleavedDoNotAffectEachOthersRolls() =>
        SeededRuns.Each(
            6,
            seed =>
            {
                var alone = new Run(seed).Play(Turns);

                var a = new Run(seed);
                var b = new Run(seed + 1000);
                for (int i = 0; i < Turns; i++)
                {
                    _ = a.Step();
                    _ = b.Step();
                }

                Assert.Equal(alone.Transcript, a.Transcript);
            }
        );
}
