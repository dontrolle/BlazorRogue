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
    public Map Map { get; private set; }

    public IFightingSystem FightingSystem { get; private set; }
    public Configuration Configuration { get; private set; }
    public EffectsSystem EffectsSystem { get; private set; }

    public int CurrentLevelNumber { get; private set; }

    // Levels already visited this playthrough, keyed by level number, so TransitionToLevel can
    // restore one exactly as it was left instead of regenerating it. Only 3 levels are defined in
    // Data/levels.json today, so this is unbounded rather than evicted - even a much deeper dungeon
    // would be trivial next to a session's other memory use.
    readonly Dictionary<int, Map> visitedLevels = [];

    const int MaxMessages = 5;
    internal bool DebugMode;
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
    /// shared with other games - it is immutable once parsed.
    /// </summary>
    public Game(Configuration configuration)
    {
        Configuration = configuration;
        References.Configuration = Configuration;

        CurrentLevelNumber = 0;
        var level = configuration.Levels[CurrentLevelNumber];
        MapGenerator = MapGeneratorFactory.Create(level, this);

        FightingSystem = new FightingSystem(this);

        Map = MapGenerator.GenerateMap();
        References.Map = Map;

        EffectsSystem = new EffectsSystem();
        References.EffectsSystem = EffectsSystem;

        References.Game = this;
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

        References.Map = Map;
        CurrentLevelNumber = targetLevelNumber;
        References.SoundManager.PlayBackgroundMusic(levelConfig.BackgroundSoundtrack);

        string verb = direction == StairDirection.Down ? "descend to" : "ascend to";
        AddMessage($"You {verb} {levelConfig.Name}.");
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
