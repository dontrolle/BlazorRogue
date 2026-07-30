namespace BlazorRogue.GameObjects;

class HalfWall(int x, int y, int halfWallIndex) : GameObject(x, y, "Halfwall")
{
    readonly int halfWallIndex = halfWallIndex;

    public override void Render(Map map) =>
        map.Decorations[X, Y]
            .Add(
                new Decoration(this, map.DungeonWallSet.ImageName(halfWallIndex))
                {
                    VerticalOffset = -1,
                    Character = "",
                }
            );
}
