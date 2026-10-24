using System;
using System.Collections.Generic;
using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.AI;

static class AIComponentFactory
{
    public const string DefaultId = SimpleAIComponent.ComponentId;

    static readonly Dictionary<string, Func<Map, SettingsMap, Random?, AIComponent>> Factories =
        new()
        {
            [SimpleAIComponent.ComponentId] = (map, settings, random) => new SimpleAIComponent(map),
            [RandomWalkAIComponent.ComponentId] = (map, settings, random) =>
                new RandomWalkAIComponent(map, random),
        };

    public static AIComponent Create(
        string id,
        Map map,
        SettingsMap settings,
        Random? random = null
    ) =>
        Factories.TryGetValue(id, out var factory)
            ? factory(map, settings, random)
            : throw new InvalidOperationException($"Unknown ai_component id '{id}'.");

    internal static bool IsKnown(string id) => Factories.ContainsKey(id);
}
