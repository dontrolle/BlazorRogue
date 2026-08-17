using BlazorRogue.GameObjects;

namespace BlazorRogue.World;

interface IMapGenerator
{
    Map GenerateMap(Moveable? existingPlayer = null);
}
