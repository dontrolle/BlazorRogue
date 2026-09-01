namespace BlazorRogue.World.Generation;

/// <summary>
/// An area given by coordinates, used internally by map generators.
/// </summary>
record struct Area(int XMin, int XMax, int YMin, int YMax)
{
    internal readonly int Width => XMax - XMin;
    internal readonly int Height => YMax - YMin;

    internal readonly Area CreateInnerAreaWithMargin(int margin) =>
        new(XMin + margin, XMax - margin, YMin + margin, YMax - margin);
}
