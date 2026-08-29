using System;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// Strategy for carving a single room's size and position within an area. Used by
/// <see cref="Node.CarveRooms"/> so different room shapes (rectangular, circular, cave-like,
/// pre-generated, ...) can be swapped in without changing the leaf-carving/recursion logic.
/// </summary>
interface IRoomCarver
{
    /// <summary>
    /// Carves and returns a room that fits within <paramref name="area"/>. The caller guarantees
    /// <paramref name="area"/> is at least <paramref name="minWidth"/> by
    /// <paramref name="minHeight"/>, so implementations don't need to re-validate that.
    /// </summary>
    /// <param name="area">The area the room must fit inside (margins already applied).</param>
    /// <param name="minWidth">Minimum width of the carved room.</param>
    /// <param name="minHeight">Minimum height of the carved room.</param>
    /// <param name="randomSource">Random source used to pick the room's size and position.</param>
    Room CarveRoom(Area area, int minWidth, int minHeight, Random randomSource);
}
