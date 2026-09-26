using System.Collections.Generic;
using BlazorRogue.Combat.Warhammer;
using BlazorRogue.Effects;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;
using BlazorRogue.World.Generation;

namespace BlazorRogue;

class Game
{
    public IMapGenerator MapGenerator { get; private set; }

    // internal (not private) so tests can substitute a controlled map for one this Game generated
    // itself - see e.g. BlazorRogue.Tests/World/LiquidPoolTests.cs's BareFloorMap helper.
    // References.Game.Map reads this property directly, so doing so is visible there too.
    public Map Map { get; internal set; }

    public IFightingSystem FightingSystem { get; private set; }
    public Configuration Configuration { get; private set; }
    public EffectsSystem EffectsSystem { get; private set; }

    public int CurrentLevelNumber { get; private set; }

    // Levels already visited this playthrough, keyed by level number, so TransitionToLevel can
    // restore one exactly as it was left instead of regenerating it. Only 3 levels are defined in
    // Data/levels.json today, so this is unbounded rather than evicted - even a much deeper dungeon
    // would be trivial next to a session's other memory use.
    readonly Dictionary<int, Map> visitedLevels = [];

    // Retained history, not the visible window (see GamePage.razor's MessageDisplayWindow for
    // that) - a single busy turn can now generate more than a handful of messages on its own
    // (the tick scheduler lets a fast moveable act - and narrate - several times per player
    // keypress), so this needs real headroom or GamePage's staggered reveal would be showing a
    // turn's messages that were already evicted before ever being displayed.
    const int MaxMessages = 20;

    /// <summary>
    /// <c>Game.DebugMode</c> controls various settings, e.g. verbose combat logging
    /// (dice rolls in the message log - see <c>FightingSystem</c>). Seeded from
    /// <see cref="Configuration.DebugMode"/> when the game is created;
    /// toggled in-game with Ctrl+D (see <c>GamePage.KeyUp</c>).
    /// </summary>
    internal bool DebugMode { get; set; }
    readonly List<string> messages = [];
    public IReadOnlyList<string> Messages => messages;

    /// <summary>
    /// Creates a game backed by its own freshly parsed <see cref="Entities.Configuration"/>.
    /// </summary>
    /// <remarks>
    /// The app shares a single parsed configuration (see Program.cs) and so uses
    /// <see cref="Game(Configuration)"/>; this overload exists for tests and standalone use.
    /// </remarks>
    public Game()
        : this(ParseConfiguration()) { }

    /// <summary>
    /// Creates a game using an already-parsed <paramref name="configuration"/>, which may be
    /// shared with other games - it is immutable once parsed. The starting level
    /// (<see cref="Configuration.StartingLevelNumber"/>) and the initial
    /// <see cref="DebugMode"/> both come from <c>Data/game-config.json</c> via the configuration.
    /// </summary>
    /// <param name="configuration">Already-parsed, immutable game content.</param>
    public Game(Configuration configuration)
    {
        Configuration = configuration;

        DebugMode = configuration.DebugMode;

        var level = configuration.Levels[configuration.StartingLevelNumber];
        CurrentLevelNumber = level.Number;
        MapGenerator = MapGeneratorFactory.Create(level, this);

        FightingSystem = new FightingSystem(this);

        Map = MapGenerator.GenerateMap();

        EffectsSystem = new EffectsSystem();

        // Everything above must be fully assigned first - References.Game.Map/.Configuration/
        // .EffectsSystem read straight through to this instance's own properties, so they only
        // become valid once this line runs.
        References.Game = this;
        // Add initial message for when the game starts
        AddMessage($"You arrive in the {level.Name}.");
    }

    /// <summary>
    /// Moves the existing player - stats, inventory and all - to the level in the given direction.
    /// A level visited earlier this playthrough is restored from visitedLevels exactly as it was
    /// left (monsters, doors, chests, fog of war and all); a level visited for the first time is
    /// freshly generated, same as before.
    /// </summary>
    public void TransitionToLevel(StairDirection direction)
    {
        int targetLevelNumber = CurrentLevelNumber + (direction == StairDirection.Down ? 1 : -1);
        var levelConfig = Configuration.Levels[targetLevelNumber];

        var player = Map.Player;
        Map.DetachPlayer();
        visitedLevels[CurrentLevelNumber] = Map;

        if (visitedLevels.TryGetValue(targetLevelNumber, out var cachedMap))
        {
            // Land on the stair leading back the way the player came, not wherever this level's
            // own CreateLayout() happened to place a first-time spawn - that spot has no relation
            // to the stair actually used to get here.
            var entryStair = cachedMap.GetStair(Opposite(direction));
            cachedMap.ReattachPlayer(player, entryStair.X, entryStair.Y);
            Map = cachedMap;
        }
        else
        {
            MapGenerator = MapGeneratorFactory.Create(levelConfig, this);
            Map = MapGenerator.GenerateMap(player);
        }

        CurrentLevelNumber = targetLevelNumber;
        References.SoundManager.PlayBackgroundMusic(levelConfig.BackgroundSoundtrack);

        string verb = direction == StairDirection.Down ? "descend to" : "ascend to";
        AddMessage($"You {verb} {levelConfig.Name}.");
    }

    // Where to return to when toggling out of the debug level (see ToggleDebugLevelView) - null
    // whenever the player isn't currently in it.
    int? preDebugLevelNumber;
    int preDebugLevelPlayerX;
    int preDebugLevelPlayerY;

    /// <summary>
    /// Dev-only jump into (and back out of) whichever level <see cref="Configuration.DebugLevelNumber"/>
    /// names (<c>Data/game-config.json</c>'s <c>debug_level</c> - e.g. "fence_gallery",
    /// "test_level", "liquid_edging_test_level") - Ctrl+G while <see cref="DebugMode"/> is on, see
    /// GamePage.OnKeyPress. Unlike <see cref="TransitionToLevel"/>, this isn't a stairs-direction
    /// move - a debug level need not have any stairs - so it remembers the player's exact prior
    /// level and position instead of relying on GetStair, and regenerates the debug level fresh
    /// every visit so it's always a clean, up-to-date reference rather than a stale cached one.
    /// </summary>
    /// <returns>Whether a level switch actually happened - false only when no debug level is
    /// configured, in which case a message explains that rather than silently doing nothing.</returns>
    internal bool ToggleDebugLevelView()
    {
        if (preDebugLevelNumber is null && Configuration.DebugLevelNumber is null)
        {
            AddMessage("No debug level configured - see game-config.json's 'debug_level'.");
            return false;
        }

        var player = Map.Player;
        Map.DetachPlayer();

        if (preDebugLevelNumber is int returnLevelNumber)
        {
            var returnLevelConfig = Configuration.Levels[returnLevelNumber];
            var returnMap = visitedLevels[returnLevelNumber];
            returnMap.ReattachPlayer(player, preDebugLevelPlayerX, preDebugLevelPlayerY);
            Map = returnMap;
            CurrentLevelNumber = returnLevelNumber;
            preDebugLevelNumber = null;

            References.SoundManager.PlayBackgroundMusic(returnLevelConfig.BackgroundSoundtrack);
            AddMessage($"You return to {returnLevelConfig.Name}.");
        }
        else
        {
            int debugLevelNumber = Configuration.DebugLevelNumber!.Value;
            var debugLevelConfig = Configuration.Levels[debugLevelNumber];

            preDebugLevelNumber = CurrentLevelNumber;
            preDebugLevelPlayerX = player.X;
            preDebugLevelPlayerY = player.Y;
            visitedLevels[CurrentLevelNumber] = Map;

            MapGenerator = MapGeneratorFactory.Create(debugLevelConfig, this);
            Map = MapGenerator.GenerateMap(player);
            CurrentLevelNumber = debugLevelNumber;

            References.SoundManager.PlayBackgroundMusic(debugLevelConfig.BackgroundSoundtrack);
            AddMessage($"You peek into {debugLevelConfig.Name} (debug).");
        }

        return true;
    }

    static StairDirection Opposite(StairDirection direction) =>
        direction == StairDirection.Down ? StairDirection.Up : StairDirection.Down;

    public void AddMessage(string message)
    {
        messages.Add(message);
        if (messages.Count > MaxMessages)
        {
            messages.RemoveAt(0);
        }
    }

    static Configuration ParseConfiguration()
    {
        var configuration = new Configuration();

        configuration.Parse();
        return configuration;
    }
}
