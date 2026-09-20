using BlazorRogue.Effects;

namespace BlazorRogue;

/// <summary>
/// Static hub the engine's leaf classes (GameObject, Moveable, Door, Chest, AIComponent, ...) reach
/// game state through when they hold no reference of their own to the active Game/Map. Blazor
/// Server can have several browser sessions' games alive in the same process at once (see
/// Sessions/GameSession), so <see cref="Game"/> must be re-pointed at the session actually being
/// served before any game logic runs for it - see <see cref="Sessions.GameSession.Activate"/>.
/// </summary>
/// <remarks>
/// <see cref="Game"/> and <see cref="SoundManager"/> are the only two members here, and both are
/// independently settable - there's nothing else to add a shortcut for. Anything that's really just
/// <see cref="Game"/>'s own state (<c>Game.Map</c>, <c>Game.Configuration</c>,
/// <c>Game.EffectsSystem</c>, ...) is reached by going through <see cref="Game"/>, not by giving it
/// a second static of its own - that used to exist for a few of them, set alongside Game at every
/// one of Game's construction/transition sites (and again in GameSession.Activate), and the
/// duplicated bookkeeping could (and once, for a related reason - see
/// BlazorRogue.Tests/Combat/CombatComponentTests.cs's ApplyDamageKillsOwnerWhenWoundsReachZero -
/// did) leave one of them stale relative to Game. <see cref="SoundManager"/> is the one genuine
/// exception: it wraps a circuit-scoped IJSRuntime that outlives no single Game, so it can't be
/// reached through Game at all.
/// </remarks>
static class References
{
    // null! avoids forcing nullable-checks throughout the codebase for values that are always
    // non-null in practice - Game is set up during its own constructor, before any game logic runs.
    public static Game Game { get; internal set; } = null!;
    public static SoundManager SoundManager { get; internal set; } = null!;
}
