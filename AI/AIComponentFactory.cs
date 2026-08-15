using System;
using System.Collections.Generic;
using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.AI;

static class AIComponentFactory
{
    public const string DefaultId = SimpleAIComponent.ComponentId;

    static readonly Dictionary<string, Func<Map, SettingsMap, AIComponent>> Factories = new()
    {
        [SimpleAIComponent.ComponentId] = (map, settings) => new SimpleAIComponent(map),
        [RandomWalkAIComponent.ComponentId] = (map, settings) => new RandomWalkAIComponent(map),
    };

    public static AIComponent Create(string id, Map map, SettingsMap settings) =>
        Factories.TryGetValue(id, out var factory)
            ? factory(map, settings)
            : throw new InvalidOperationException($"Unknown ai_component id '{id}'.");

    internal static bool IsKnown(string id) => Factories.ContainsKey(id);
}
