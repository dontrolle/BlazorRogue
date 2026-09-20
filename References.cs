using BlazorRogue.Effects;
using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue;

/// <summary>
/// Static hub the engine's leaf classes (GameObject, Moveable, Door, Chest, AIComponent, ...) reach
/// game state through when they hold no reference of their own to the active Game/Map. Blazor
/// Server can have several browser sessions' games alive in the same process at once (see
/// Sessions/GameSession), so <see cref="Game"/> must be re-pointed at the session actually being
/// served before any game logic runs for it - see <see cref="Sessions.GameSession.Activate"/>.
/// </summary>
/// <remarks>
/// Only <see cref="Game"/> and <see cref="SoundManager"/> are independently settable.
/// <see cref="SoundManager"/> is genuinely separate - it wraps a circuit-scoped IJSRuntime that
/// outlives no single Game. <see cref="Map"/>/<see cref="Configuration"/>/<see cref="EffectsSystem"/>
/// are plain pass-throughs onto <see cref="Game"/>'s own properties rather than independently-set
/// statics: each used to be its own static, set alongside Game at every one of Game's
/// construction/transition sites (and again in GameSession.Activate) - duplicated bookkeeping that
/// could (and once, for a related reason - see BlazorRogue.Tests/Combat/CombatComponentTests.cs's
/// ApplyDamageKillsOwnerWhenWoundsReachZero - did) leave one of them stale relative to Game.
/// </remarks>
static class References
{
    // null! avoids forcing nullable-checks throughout the codebase for values that are always
    // non-null in practice - Game is set up during its own constructor, before any game logic runs.
    public static Game Game { get; internal set; } = null!;
    public static SoundManager SoundManager { get; internal set; } = null!;

    public static Map Map => Game.Map;
    public static Configuration Configuration => Game.Configuration;
    public static EffectsSystem EffectsSystem => Game.EffectsSystem;
}
