using BlazorRogue.Combat.Warhammer;
using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue;

class Game
{
    const int Width = 60;
    const int Height = 45;

    public IDungeonGenerator DungeonGenerator { get; private set; }
    public Map Map { get; private set; }

    public IFightingSystem FightingSystem { get; private set; }
    public Configuration Configuration { get; private set; }
    public EffectsSystem EffectsSystem { get; private set; }

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

        DungeonGenerator = new DungeonGenerator(Width, Height, this);
        FightingSystem = new FightingSystem(this);

        Map = DungeonGenerator.GenerateMap();
        References.Map = Map;

        EffectsSystem = new EffectsSystem();
        References.EffectsSystem = EffectsSystem;
    }

    static Configuration ParseConfiguration()
    {
        var configuration = new Configuration();

        // TODO: pass as async
        configuration.Parse();
        return configuration;
    }
}
