using System;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// Wraps a <paramref name="primary"/> <see cref="IRoomCarver"/> and retries with
/// <paramref name="fallback"/> if the primary throws <see cref="InvalidOperationException"/> - the
/// failure <see cref="CaveRoomCarver"/> reports when its cellular automaton leaves a leaf with no
/// floor cell to anchor a connector point on. Lets an unreliable carver be offered even for small
/// leaf areas without one bad roll aborting the whole map generation.
/// </summary>
/// <param name="primary">Carver to try first.</param>
/// <param name="fallback">
/// Carver to fall back to; assumed reliable (e.g. <see cref="RectangularRoomCarver"/>). If it also
/// throws, that propagates.
/// </param>
class FallbackRoomCarver(IRoomCarver primary, IRoomCarver fallback) : IRoomCarver
{
    public Room CarveRoom(Area area, int minWidth, int minHeight, Random randomSource)
    {
        try
        {
            return primary.CarveRoom(area, minWidth, minHeight, randomSource);
        }
        catch (InvalidOperationException)
        {
            return fallback.CarveRoom(area, minWidth, minHeight, randomSource);
        }
    }
}
