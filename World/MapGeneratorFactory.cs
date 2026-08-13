using System;
using System.Collections.Generic;
using BlazorRogue;
using BlazorRogue.Entities;
using BlazorRogue.World;

static class MapGeneratorFactory
{
    static readonly Dictionary<string, Func<int, int, Game, IMapGenerator>> Factories = new()
    {
        [BasicDungeonGenerator.Id] = (w, h, g) => new BasicDungeonGenerator(w, h, g),
        [CaveGenerator.Id] = (w, h, g) => new CaveGenerator(w, h, g),
    };

    public static IMapGenerator Create(LevelConfiguration level, Game game) =>
        Factories.TryGetValue(level.MapGeneratorId, out var factory)
            ? factory(level.Width, level.Height, game)
            : throw new InvalidOperationException(
                $"Unknown map generator id '{level.MapGeneratorId}' for level '{level.Id}'."
            );

    internal static bool IsKnown(string id) => Factories.ContainsKey(id);
}
