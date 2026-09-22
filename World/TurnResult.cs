using System.Collections.Generic;
using BlazorRogue.AI;
using BlazorRogue.Combat.Warhammer;
using BlazorRogue.GameObjects;

namespace BlazorRogue.World;

/// <summary>
/// Everything that happened during one call to <see cref="Map.TakeTurn"/>: the player's own
/// action and (if it was a melee attack) its outcome, every monster action resolved that turn
/// (in resolution order - see <see cref="Map.PlayerTookTurn"/>'s tick-priority-queue drain, which
/// can give a fast moveable more than one entry here and a slow one none), and the
/// game-over/level-change flags a caller (a headless driver, or GamePage.razor once it switches
/// over) needs to react to. Doesn't itemize every ability's side effect (e.g. a push-back's new
/// coordinates) - those narrate themselves via Game.Messages; this stays scoped to the outcomes a
/// caller might actually need to branch on.
/// </summary>
record struct TurnResult(
    bool TurnConsumed,
    PlayerAction RequestedAction,
    AttackResult? PlayerAttack,
    IReadOnlyList<MonsterAction> MonsterActions,
    bool GameOverThisTurn,
    bool LevelChangedThisTurn
);

/// <summary>
/// One moveable's move or attack during a single <see cref="Map.PlayerTookTurn"/> - a
/// <see cref="AITurnOutcome.DidNothing"/> outcome (asleep/blocked/stumbled) never produces one of
/// these, matching the old null-returning convention it replaces.
/// </summary>
record struct MonsterAction(Moveable Actor, AITurnOutcome Outcome);
