using System.Collections.Generic;
using BlazorRogue.Combat.Warhammer;
using BlazorRogue.GameObjects;

namespace BlazorRogue.World;

/// <summary>
/// Everything that happened during one call to <see cref="Map.TakeTurn"/>: the player's own
/// action and (if it was a melee attack) its outcome, every monster's attack that turn, and the
/// game-over/level-change flags a caller (a headless driver, or GamePage.razor once it switches
/// over) needs to react to. Doesn't itemize every ability's side effect (e.g. a push-back's new
/// coordinates) - those narrate themselves via Game.Messages; this stays scoped to the outcomes a
/// caller might actually need to branch on.
/// </summary>
record struct TurnResult(
    bool TurnConsumed,
    PlayerAction RequestedAction,
    AttackResult? PlayerAttack,
    IReadOnlyList<MonsterAttack> MonsterAttacks,
    bool GameOverThisTurn,
    bool LevelChangedThisTurn
);

/// <summary>One monster's attack against the player during a single <see cref="Map.PlayerTookTurn"/>.</summary>
record struct MonsterAttack(Moveable Attacker, AttackResult Result);
