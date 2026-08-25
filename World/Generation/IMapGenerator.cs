using BlazorRogue.GameObjects;

namespace BlazorRogue.World.Generation;

/// <summary>
/// Interface for map-generators.
///
/// Should respect configuration for levels and generate a complete map,
/// including stairs to connect to other levels, etc. See <see cref="MapGeneratorBase"/>
/// for a convenient abstract class implementation that takes relevant configuration
/// and provides utility methods and overridable worker-methods.
/// </summary>
interface IMapGenerator
{
    /// <summary>
    /// Generate and return a <see cref="Map" />.
    ///
    /// May take an existing player object and place it, and else should generate a player, place it and set it
    /// in <see cref="Map.Player"/>.
    /// </summary>
    /// <param name="existingPlayer">An existing player, if relevant</param>
    /// <returns>The generated map</returns>
    Map GenerateMap(Moveable? existingPlayer = null);
}
