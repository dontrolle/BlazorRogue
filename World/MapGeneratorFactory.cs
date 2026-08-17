using System;
using System.Collections.Generic;
using BlazorRogue;
using BlazorRogue.Entities;
using BlazorRogue.World;

static class MapGeneratorFactory
{
    static readonly Dictionary<
        string,
        Func<int, int, int, Game, SettingsMap, IMapGenerator>
    > Factories = new()
    {
        [BasicDungeonGenerator.Id] = (w, h, n, g, s) => new BasicDungeonGenerator(w, h, n, g, s),
        [CaveGenerator.Id] = (w, h, n, g, s) => new CaveGenerator(w, h, n, g, s),
    };

    public static IMapGenerator Create(LevelConfiguration level, Game game) =>
        Factories.TryGetValue(level.MapGeneratorId, out var factory)
            ? factory(level.Width, level.Height, level.Number, game, level.SettingsMap)
            : throw new InvalidOperationException(
                $"Unknown map generator id '{level.MapGeneratorId}' for level '{level.Id}'."
            );

    internal static bool IsKnown(string id) => Factories.ContainsKey(id);
}
