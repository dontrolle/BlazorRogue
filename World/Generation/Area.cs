namespace BlazorRogue.World.Generation;

class Area(int xMin, int xMax, int yMin, int yMax)
{
    internal readonly int XMin = xMin;
    internal readonly int XMax = xMax;
    internal readonly int YMin = yMin;
    internal readonly int YMax = yMax;

    internal int Width => XMax - XMin;
    internal int Height => YMax - YMin;
}