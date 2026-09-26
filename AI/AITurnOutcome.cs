using BlazorRogue.Combat.Warhammer;

namespace BlazorRogue.AI;

/// <summary>
/// What a single <see cref="AIComponent.TakeTurn"/> call actually did - the result of one action
/// slot in <see cref="World.Map"/>'s tick-priority-queue scheduler. Every call
/// represents a real, due turn (the scheduler never invokes a sleeping or already-resolved
/// moveable), so this is guaranteed non-null - a moveable that couldn't act still reports
/// <see cref="DidNothing"/> rather than nothing at all.
/// </summary>
abstract record AITurnOutcome
{
    AITurnOutcome() { }

    /// <summary>Moved to (X, Y) - the moveable's own new position.</summary>
    internal sealed record Moved(int X, int Y) : AITurnOutcome;

    /// <summary>Made a melee attack, regardless of whether it hit.</summary>
    internal sealed record Attacked(AttackResult Result) : AITurnOutcome;

    /// <summary>Asleep, blocked with nothing to attack, or stumbled in slow liquid.</summary>
    internal sealed record DidNothing : AITurnOutcome;
}
