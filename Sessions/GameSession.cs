using System;
using BlazorRogue.Effects;
using BlazorRogue.Entities;

namespace BlazorRogue.Sessions;

/// <summary>
/// One browser's play session: the game currently in progress, plus the view preferences that
/// should outlive a page reload.
/// </summary>
/// <remarks>
/// A session deliberately outlives the Blazor circuit rendering it - that is what makes a game
/// survive a reload - so it must never hold circuit-scoped state. In particular
/// <see cref="SoundManager"/> wraps an <c>IJSRuntime</c> belonging to a single circuit,
/// so it is passed in to <see cref="Activate"/> per circuit rather than stored here.
/// </remarks>
sealed class GameSession
{
    readonly Configuration configuration;
    readonly TimeProvider timeProvider;
    readonly string? startingLevelId;

    internal GameSession(
        string id,
        Configuration configuration,
        TimeProvider timeProvider,
        string? startingLevelId = null
    )
    {
        Id = id;
        this.configuration = configuration;
        this.timeProvider = timeProvider;
        this.startingLevelId = startingLevelId;

        LastAccessed = timeProvider.GetUtcNow();
        Game = new Game(configuration, startingLevelId);
    }

    public string Id { get; }

    /// <summary>
    /// The game in progress. Replaced wholesale by <see cref="StartNewGame"/>, so callers must
    /// re-read it afterwards rather than holding on to an instance across that call.
    /// </summary>
    public Game Game { get; private set; }

    /// <summary>
    /// Whether the player switched to the ASCII renderer. Kept here rather than in the component
    /// so that it survives a reload along with the game.
    /// </summary>
    public bool PreferAscii { get; set; }

    /// <summary>The tile size (px) the tileset renderer starts at, before any in-game zoom.</summary>
    public const int DefaultGraphicTileSize = 48;

    /// <summary>
    /// The tileset renderer's tile size in px, adjusted in-game with +/-. Kept here (like
    /// <see cref="PreferAscii"/>) so the chosen zoom survives a page reload along with the game.
    /// The component clamps this to its own min/max on load, so an out-of-range value stored by a
    /// future build can't break the view.
    /// </summary>
    public int GraphicTileSize { get; set; } = DefaultGraphicTileSize;

    /// <summary>
    /// When this session was last created, resumed or played. Drives idle eviction in
    /// <see cref="GameSessionStore"/>.
    /// </summary>
    public DateTimeOffset LastAccessed { get; internal set; }

    /// <summary>Discards the game in progress and generates a fresh dungeon in its place.</summary>
    public void StartNewGame()
    {
        LastAccessed = timeProvider.GetUtcNow();
        Game = new Game(configuration, startingLevelId);
    }

    /// <summary>
    /// Points the static <see cref="References"/> hub at this session's game, and marks the session
    /// as currently in use.
    /// </summary>
    /// <remarks>
    /// The engine reaches game state through statics rather than through DI, so once more than one
    /// session can be alive those statics have to be re-pointed before any game logic runs -
    /// otherwise a handler mutates whichever game happened to be constructed most recently (e.g.
    /// Chest.Use() crediting gold to another player). This is safe because the game loop is fully
    /// synchronous: nothing awaits between activation and the end of the handler, so no other
    /// circuit can interleave.
    ///
    /// Constructing a <see cref="BlazorRogue.Game"/> also writes those statics as a side effect,
    /// which is the other reason every code path must activate after obtaining a session.
    /// </remarks>
    public void Activate(SoundManager soundManager)
    {
        LastAccessed = timeProvider.GetUtcNow();

        References.Game = Game;
        References.Configuration = Game.Configuration;
        References.Map = Game.Map;
        References.EffectsSystem = Game.EffectsSystem;
        References.SoundManager = soundManager;
    }
}
