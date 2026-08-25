namespace BlazorRogue.World.Generation;

/// <summary>
/// Rectangular room class, used internally by map-generators.
/// </summary>
class Room(int x, int y, int width, int height)
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int Left => X;
    public int Right => X + Width - 1;
    public int Upper => Y;
    public int Lower => Y + Height - 1;
    public int CenterX => X + ((Width - 1) / 2);
    public int CenterY => Y + ((Height - 1) / 2);

    public Room(Area area) : this(area.XMin, area.YMin, area.Width, area.Height)
    {
    }

    public bool Intersect(Room other)
    {
        bool xInter = Left <= other.Right && Right >= other.Left;
        bool yInter = Lower >= other.Upper && Upper <= other.Lower;
        return xInter && yInter;
    }
}
