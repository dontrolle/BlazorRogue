using System.Collections.Generic;

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

    public Room(Area area)
        : this(area.XMin, area.YMin, area.Width, area.Height) { }

    public bool Intersect(Room other)
    {
        bool xInter = Left <= other.Right && Right >= other.Left;
        bool yInter = Lower >= other.Upper && Upper <= other.Lower;
        return xInter && yInter;
    }

    /// <summary>
    /// The room's floor footprint, as one or more (possibly overlapping) rectangular areas.
    /// Code that needs to know exactly which cells are floor - as opposed to just this room's
    /// bounding box - should iterate this instead of <see cref="Left"/>/<see cref="Right"/>/
    /// <see cref="Upper"/>/<see cref="Lower"/>. Defaults to a single area matching the room's own
    /// bounding box; overridden by room shapes whose footprint isn't just a solid rectangle.
    /// </summary>
    internal virtual IEnumerable<Area> FootprintAreas => [new Area(X, X + Width, Y, Y + Height)];

    /// <summary>
    /// The point corridors should connect to for this room. Defaults to the bounding box's
    /// geometric center (<see cref="CenterX"/>/<see cref="CenterY"/>), which for a plain
    /// rectangular room is always on its floor. Room shapes whose footprint isn't just their
    /// bounding box - so the center might land outside it - should override this with a point
    /// from <see cref="FootprintAreas"/> instead; it's also a natural hook for shapes that want to
    /// pick their connector point some other way entirely (off-center, weighted toward one part of
    /// the room, etc.), not just for guaranteeing floor.
    /// </summary>
    internal virtual GridPoint ConnectorPoint => new(CenterX, CenterY);

    /// <summary>
    /// What kind of room this is - see <see cref="RoomType"/>. Defaults to
    /// <see cref="RoomType.Rectangular"/>; overridden by room shapes that aren't a plain
    /// rectangle. Purely descriptive: carving code sets it, but doesn't interpret it - it's for
    /// callers that want to make a decision (e.g. picking a tileset) based on room shape.
    /// </summary>
    internal virtual RoomType Type => RoomType.Rectangular;
}
