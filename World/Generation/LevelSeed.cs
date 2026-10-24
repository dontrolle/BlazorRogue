using System;

namespace BlazorRogue.World.Generation;

/// <summary>
/// Derives each level's random seeds from the game's seed, so a seeded game generates the same
/// dungeon whatever order its levels are visited in. Deliberately not <c>HashCode.Combine</c>:
/// that is randomized per process, which would defeat reproducibility across runs.
/// </summary>
static class LevelSeed
{
    /// <summary>Layout and content picks made while a generator runs.</summary>
    public const int Generation = 0;

    /// <summary>Picks a generator makes before its own constructor has run (e.g. wall-set).</summary>
    public const int Setup = 1;

    /// <summary>Another pick of the same kind as <see cref="Setup"/> - kept apart so the two don't correlate.</summary>
    public const int SetupFloor = 2;

    /// <summary>Cosmetic sprite picks made while rendering (see <see cref="Map.Random"/>), apart so they never shift the layout.</summary>
    public const int Cosmetic = 3;

    /// <summary>
    /// Reserved for the game-wide gameplay stream (<see cref="Gameplay"/>); no level stream may use
    /// it, so the gameplay seed never equals any level's seed, whatever the level's number.
    /// </summary>
    const int GameplayStream = 4;

    /// <summary>
    /// First stream id of the per-monster streams (<see cref="Monster"/>): one per monster spawned
    /// on a level, so adding a monster doesn't shift any other monster's rolls.
    /// </summary>
    const int FirstMonsterStream = 1000;

    /// <summary>The stream id of the <paramref name="spawnIndex"/>th monster spawned on a level.</summary>
    public static int Monster(int spawnIndex) => FirstMonsterStream + spawnIndex;

    /// <summary>
    /// The seed of a game's gameplay stream (dice, and the root the per-monster streams derive from),
    /// kept apart from every level stream so how many dice were rolled never changes a level.
    /// </summary>
    public static int Gameplay(int gameSeed) => For(gameSeed, 0, GameplayStream);

    /// <summary>
    /// A seed for one of a level's random streams, mixed (splitmix64-style) from the game seed,
    /// the level number and the stream. Always non-negative.
    /// </summary>
    public static int For(int gameSeed, int levelNumber, int stream)
    {
        unchecked
        {
            ulong z =
                ((uint)gameSeed * 0x9E3779B97F4A7C15UL)
                + ((uint)levelNumber * 0xBF58476D1CE4E5B9UL)
                + ((uint)stream * 0x94D049BB133111EBUL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (int)(z >> 33);
        }
    }

    public static Random Random(int gameSeed, int levelNumber, int stream) =>
        new(For(gameSeed, levelNumber, stream));
}
