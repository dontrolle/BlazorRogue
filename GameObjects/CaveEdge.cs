using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

class CaveEdge(int x, int y, int caveEdgeIndex, int offset, int hOffset)
    : GameObject(x, y, "CaveEdge")
{
    readonly int caveEdgeIndex = caveEdgeIndex;
    readonly int offset = offset;
    readonly int hOffset = hOffset;

    public override void Render(Map map) =>
        map.Decorations[X, Y]
            .Add(
                new Decoration(this, "wall_cave_edge_" + caveEdgeIndex)
                {
                    HorizontalOffset = hOffset,
                    VerticalOffset = offset,
                }
            );
}
