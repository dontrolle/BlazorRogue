using System;
using System.Collections.Generic;
using BlazorRogue.Entities;
using BlazorRogue.World.Generation.BSPGenerator;

namespace BlazorRogue.World.Generation;

/// <summary>
/// Factory class for creating concrete map-generator instances.
/// </summary>
static class MapGeneratorFactory
{
    static readonly Dictionary<
        string,
        Func<int, int, int, Game, SettingsMap, IMapGenerator>
    > Factories = new()
    {
        [BasicDungeonGenerator.Id] = (w, h, n, g, s) => new BasicDungeonGenerator(w, h, n, g, s),
        [BSPMapGenerator.Id] = (w, h, n, g, s) => new BSPMapGenerator(w, h, n, g, s),
        [CaveGenerator.Id] = (w, h, n, g, s) => new CaveGenerator(w, h, n, g, s),
        [TestMapGenerator.Id] = (w, h, n, g, s) => new TestMapGenerator(w, h, n, g, s),
    };

    /// <summary>
    /// Factory method for creating concrete map-generators from their string-id.
    /// </summary>
    /// <param name="level">LevelConfiguration instance</param>
    /// <param name="game">Game instance</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown if no map-generator exists matching the generator-id given in <c><paramref name="level"/>.MapGeneratorId</c></exception>
    public static IMapGenerator Create(LevelConfiguration level, Game game) =>
        Factories.TryGetValue(level.MapGeneratorId, out var factory)
            ? factory(level.Width, level.Height, level.Number, game, level.SettingsMap)
            : throw new InvalidOperationException(
                $"Unknown map generator id '{level.MapGeneratorId}' for level '{level.Id}'."
            );

    internal static bool IsKnown(string id) => Factories.ContainsKey(id);
}
