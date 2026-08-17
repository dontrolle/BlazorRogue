using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

class WallEdge(int x, int y, TileSet wallSet, int edgeIndex, int offset, int hOffset)
    : GameObject(x, y, "WallEdge")
{
    readonly TileSet wallSet = wallSet;
    readonly int edgeIndex = edgeIndex;
    readonly int offset = offset;
    readonly int hOffset = hOffset;

    public override void Render(Map map) =>
        map.Decorations[X, Y]
            .Add(
                new Decoration(this, wallSet.EdgeImageName(edgeIndex))
                {
                    HorizontalOffset = hOffset,
                    VerticalOffset = offset,
                }
            );
}
