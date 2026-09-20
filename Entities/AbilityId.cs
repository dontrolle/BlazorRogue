namespace BlazorRogue.Entities;

/// <summary>
/// A special ability a <see cref="GameObjects.Moveable"/> can be tagged with, parsed from a
/// moveable's optional <c>abilities</c> array in <c>Data/monsters.json</c>/<c>Data/heroes.json</c>
/// (each entry <c>{"id": ..., "parameters": {...}}</c>) - see <see cref="Components.AbilitiesComponent"/>.
/// Unlike <see cref="AI.AIComponent"/>, a moveable can carry any number of these at once.
/// </summary>
enum AbilityId
{
    /// <summary>Unaffected by liquid-pool ground effects (instakill/acid/slow) and blocked edges (fences) - see <see cref="World.Map.IsFlying"/>.</summary>
    Flying,

    /// <summary>Chance (<c>"chance"</c> parameter, 0-100) to shove the defender one tile back on a successful hit - see <see cref="Combat.Warhammer.FightingSystem"/>.</summary>
    PushBack,
}
