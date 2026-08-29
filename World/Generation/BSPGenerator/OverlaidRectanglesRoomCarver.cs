using System;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// Carves a room out of two overlapping rectangles: a wide/short bar and a narrow/tall bar,
/// crossed into a "plus" shape. Both bars are positioned to cover the same random anchor point,
/// guaranteeing they overlap into a single connected room. Biasing the two bars' aspect ratios
/// this way - rather than picking two independently-sized rectangles - keeps them visually
/// distinct even in small leaf areas, where two similarly-sized random rectangles would tend to
/// just coincide.
/// <para>
/// Insets its area by one extra cell on top of whatever margin the caller already applied (via
/// <see cref="Node.CarveRooms"/>'s <c>minDistanceToDivider</c>), so these rooms sit a bit further
/// from the leaf's dividers than a plain rectangular room would. Because of that extra inset, the
/// area handed in needs at least <c>minWidth + 2</c> by <c>minHeight + 2</c> cells to carve
/// successfully.
/// </para>
/// </summary>
class OverlaidRectanglesRoomCarver : IRoomCarver
{
    internal static readonly OverlaidRectanglesRoomCarver Instance = new();

    const int ExtraMarginToDivider = 1;

    public Room CarveRoom(Area area, int minWidth, int minHeight, Random randomSource)
    {
        var inset = area.CreateInnerAreaWithMargin(ExtraMarginToDivider);

        int anchorX = randomSource.Next(inset.XMin, inset.XMax);
        int anchorY = randomSource.Next(inset.YMin, inset.YMax);

        int horizontalHeight = randomSource.Next(
            minHeight,
            Math.Max(minHeight, inset.Height / 2) + 1
        );
        int horizontalWidth = randomSource.Next(minWidth, inset.Width + 1);
        var horizontalBar = RectCoveringAnchor(
            inset,
            horizontalWidth,
            horizontalHeight,
            anchorX,
            anchorY,
            randomSource
        );

        int verticalWidth = randomSource.Next(minWidth, Math.Max(minWidth, inset.Width / 2) + 1);
        int verticalHeight = randomSource.Next(minHeight, inset.Height + 1);
        var verticalBar = RectCoveringAnchor(
            inset,
            verticalWidth,
            verticalHeight,
            anchorX,
            anchorY,
            randomSource
        );

        return new OverlaidRoom(horizontalBar, verticalBar, new GridPoint(anchorX, anchorY));
    }

    static Area RectCoveringAnchor(
        Area inset,
        int width,
        int height,
        int anchorX,
        int anchorY,
        Random randomSource
    )
    {
        int xMin = PositionCoveringAnchor(inset.XMin, inset.XMax, width, anchorX, randomSource);
        int yMin = PositionCoveringAnchor(inset.YMin, inset.YMax, height, anchorY, randomSource);
        return new Area(xMin, xMin + width, yMin, yMin + height);
    }

    /// <summary>
    /// Picks a random start position for a span of <paramref name="size"/> cells within
    /// <c>[min, max)</c> that is guaranteed to cover <paramref name="anchor"/> (itself assumed to
    /// lie within <c>[min, max)</c>, with <paramref name="size"/> at most <c>max - min</c>).
    /// </summary>
    static int PositionCoveringAnchor(int min, int max, int size, int anchor, Random randomSource)
    {
        int lowestStart = Math.Max(min, anchor - size + 1);
        int highestStart = Math.Min(max - size, anchor);
        return randomSource.Next(lowestStart, highestStart + 1);
    }
}
