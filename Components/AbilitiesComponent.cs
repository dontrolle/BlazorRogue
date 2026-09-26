using System;
using System.Collections.Generic;
using BlazorRogue.Entities;

namespace BlazorRogue.Components;

/// <summary>
/// Holds the (possibly empty) set of <see cref="AbilityId"/>s a <see cref="GameObjects.Moveable"/>
/// was tagged with in its <see cref="MoveableType"/>, each with its own parsed
/// <see cref="SettingsMap"/> of parameters. Unlike the other components, this is purely data -
/// behaviour lives at the fixed call sites that check for a specific ability (see
/// <see cref="World.Map.IsFlying"/> and <see cref="Combat.Warhammer.FightingSystem"/>).
/// </summary>
class AbilitiesComponent(IReadOnlyDictionary<AbilityId, SettingsMap> abilities) : Component
{
    public bool Has(AbilityId id) => abilities.ContainsKey(id);

    public SettingsMap GetParameters(AbilityId id) =>
        abilities.TryGetValue(id, out var settings)
            ? settings
            : throw new InvalidOperationException($"Ability '{id}' is not present.");
}
