using System.Collections.Generic;
using BlazorRogue.Combat.Warhammer;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue;

class Game
{
    public IMapGenerator MapGenerator { get; private set; }
    public Map Map { get; private set; }

    public IFightingSystem FightingSystem { get; private set; }
    public Configuration Configuration { get; private set; }
    public EffectsSystem EffectsSystem { get; private set; }

    public int CurrentLevelNumber { get; private set; }

    const int MaxMessages = 5;
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
    /// Regenerates the level in the given direction from scratch (nothing about a level persists
    /// between visits) and moves the existing player - stats, inventory and all - onto it.
    /// </summary>
    public void TransitionToLevel(StairDirection direction)
    {
        int targetLevelNumber = CurrentLevelNumber + (direction == StairDirection.Down ? 1 : -1);
        var levelConfig = Configuration.Levels[targetLevelNumber];

        Map.DetachPlayer();
        var player = Map.Player;

        MapGenerator = MapGeneratorFactory.Create(levelConfig, this);
        Map = MapGenerator.GenerateMap(player);
        References.Map = Map;
        CurrentLevelNumber = targetLevelNumber;

        string verb = direction == StairDirection.Down ? "descend to" : "ascend to";
        AddMessage($"You {verb} {levelConfig.Name}.");
    }

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
