using BlazorRogue.World;

namespace BlazorRogue.Tests.TestSupport;

/// <summary>
/// Thin wrapper for driving a <see cref="BlazorRogue.Game"/> with no Blazor Server/browser
/// involved (issue #88) - fast automated play tests and, eventually, play-balance sweeps. Every
/// actual turn-taking rule lives in <see cref="Map.TakeTurn"/>; this only owns the
/// <see cref="Game"/> instance and forwards to it, plus optionally driving an
/// <see cref="IPlayerPolicy"/> instead of a caller picking each <see cref="PlayerAction"/> by hand.
/// Reuses <see cref="FakeJsRuntime"/>'s existing <c>[ModuleInitializer]</c>-wired
/// <see cref="BlazorRogue.Effects.SoundManager"/> - no separate JS-interop plumbing needed.
/// </summary>
/// <remarks>
/// Wraps an already-constructed <paramref name="game"/> - e.g. one built over a hand-rolled
/// bare floor map, the same technique <c>TurnResultTests.BareFloorMap</c> uses, for tests that
/// need deterministic layout rather than a real generated dungeon.
/// </remarks>
sealed class HeadlessPlayDriver(Game game)
{
    public Game Game { get; } = game;

    /// <summary>
    /// The current map - reads straight through to <see cref="BlazorRogue.Game.Map"/>, so it
    /// already reflects a level transition mid-game (e.g. taking stairs).
    /// </summary>
    public Map Map => Game.Map;

    /// <summary>Wraps a freshly generated <see cref="BlazorRogue.Game"/>.</summary>
    public HeadlessPlayDriver()
        : this(new Game()) { }

    public TurnResult TakeTurn(PlayerAction action) => Map.TakeTurn(action);

    /// <summary>Asks <paramref name="policy"/> what to do next, then takes that turn.</summary>
    public TurnResult TakeTurn(IPlayerPolicy policy) => TakeTurn(policy.NextAction(Map));
}
