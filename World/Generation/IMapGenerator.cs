using BlazorRogue.GameObjects;

namespace BlazorRogue.World.Generation;

interface IMapGenerator
{
    Map GenerateMap(Moveable? existingPlayer = null);
}
