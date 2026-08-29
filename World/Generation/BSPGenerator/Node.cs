using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// A node in a binary space partition tree: wraps a rectangular <see cref="Generation.Area"/> that
/// is either a leaf (not yet split) or has been divided into <see cref="Left"/> and
/// <see cref="Right"/> children covering the same area.
/// </summary>
class Node(Area area, int id = 0)
{
    /// <summary>
    /// Debug-only identifier for use in tests; assigned via the standard 0-indexed binary heap scheme so expected values can be computed by hand. Not intended as a stable identity for gameplay logic (corridors, persistence, etc.) — use object reference for that.
    /// </summary>
    internal int Id => id;
    internal Room? Room;

    internal Corridor? Corridor;
    internal Node? Left;
    internal Node? Right;
    internal Area Area => area;

    /// <summary>
    /// Recursively splits this node and its descendants along a randomly chosen axis, stopping
    /// once a node's area is smaller than <paramref name="threshold"/> in both dimensions.
    /// </summary>
    /// <param name="threshold">
    /// A node stops splitting further once both its width and height fall below this size.
    /// </param>
    /// <param name="minSplit">
    /// Minimum distance from either edge of a node's area to its split point, so both resulting
    /// children are at least this large along the split axis. Must be at most half of
    /// <paramref name="threshold"/>.
    /// </param>
    /// <param name="randomSource">
    /// Random source used to pick various properties, e.g. split axis and split positions.+
    /// </param>
    /// <param name="maxSplitOffsetFromCenterProportion">
    /// If given, a number between 0 and 0.5 that indicates the maximum proportion from center
    ///  that the split can be. E.g., =0.05 means the split will be between 0.45 and 0.55 from the
    /// center.
    /// </param>
    /// <param name="earlyExitChance">
    /// Raw chance [0,1] of returning early, making the current node a leaf.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="threshold"/> is less than <c>2 * minSplit</c>.
    /// </exception>
    internal void SplitUntilThreshold(
        int threshold,
        int minSplit,
        Random randomSource,
        double? maxSplitOffsetFromCenterProportion = null,
        double earlyExitChance = 0
    )
    {
        if (maxSplitOffsetFromCenterProportion is < 0 or > 0.5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSplitOffsetFromCenterProportion),
                maxSplitOffsetFromCenterProportion,
                "must be a value between 0 and 0.5."
            );
        }

        if (earlyExitChance is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(earlyExitChance),
                earlyExitChance,
                "must be a value between 0 and 1."
            );
        }

        if (threshold < 2 * minSplit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                threshold,
                $"threshold must be at least 2 * minSplit ({2 * minSplit}), otherwise a split "
                    + "point satisfying minSplit on both sides may not exist."
            );
        }

        bool mayHorizontalSplit = area.Height >= threshold;
        bool mayVerticalSplit = area.Width >= threshold;

        // are we done?
        if (randomSource.NextDouble() < earlyExitChance)
            return;

        if (!mayHorizontalSplit && !mayVerticalSplit)
        {
            // we are at a leaf
            return;
        }

        bool horizontalSplit;
        if (!mayVerticalSplit)
        {
            horizontalSplit = true; // width already too small - forced to split height
        }
        else if (!mayHorizontalSplit)
        {
            horizontalSplit = false; // height already too small - forced to split width
        }
        else
        {
            horizontalSplit = randomSource.Next(0, 2) == 1; // both viable - pick randomly
        }

        if (horizontalSplit)
        {
            int minY = area.YMin + minSplit;
            int maxY = area.YMax - minSplit;
            if (maxSplitOffsetFromCenterProportion.HasValue)
            {
                int maxYOffset = maxY - minY;
                int origMinY = minY;
                minY = (int)
                    double.Round(
                        origMinY + (maxYOffset * (0.5 - maxSplitOffsetFromCenterProportion.Value))
                    );
                maxY = (int)
                    double.Round(
                        origMinY + (maxYOffset * (0.5 + maxSplitOffsetFromCenterProportion.Value))
                    );
            }

            int yPos = randomSource.Next(minY, maxY);
            SplitHorizontalAt(yPos);
        }
        else
        {
            int minX = area.XMin + minSplit;
            int maxX = area.XMax - minSplit;
            if (maxSplitOffsetFromCenterProportion.HasValue)
            {
                int maxXOffset = maxX - minX;
                int origMinX = minX;
                minX = (int)
                    double.Round(
                        origMinX + (maxXOffset * (0.5 - maxSplitOffsetFromCenterProportion.Value))
                    );
                maxX = (int)
                    double.Round(
                        origMinX + (maxXOffset * (0.5 + maxSplitOffsetFromCenterProportion.Value))
                    );
            }

            int xPos = randomSource.Next(minX, maxX);
            SplitVerticalAt(xPos);
        }

        // and now recurse
        Left!.SplitUntilThreshold(
            threshold,
            minSplit,
            randomSource,
            maxSplitOffsetFromCenterProportion,
            earlyExitChance
        );
        Right!.SplitUntilThreshold(
            threshold,
            minSplit,
            randomSource,
            maxSplitOffsetFromCenterProportion,
            earlyExitChance
        );
    }

    int NextLeftId => (Id * 2) + 1;
    int NextRightId => (Id * 2) + 2;

    /// <summary>
    /// Splits this (not yet split) node into <see cref="Left"/> (top) and <see cref="Right"/>
    /// (bottom) children, cutting the area with a horizontal line at <paramref name="yPos"/>.
    /// </summary>
    /// <param name="yPos">Y-coordinate of the cut line; must lie strictly within the area.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="yPos"/> does not lie strictly within the area's YMin/YMax.
    /// </exception>
    /// <exception cref="InvalidOperationException">This node has already been split.</exception>
    internal void SplitHorizontalAt(int yPos)
    {
        ValidateNoExistingSplit();

        if (yPos <= area.YMin || yPos >= area.YMax)
        {
            throw new ArgumentOutOfRangeException(nameof(yPos), "Out of bounds");
        }

        Left = new Node(new Area(area.XMin, area.XMax, area.YMin, yPos), NextLeftId);
        Right = new Node(new Area(area.XMin, area.XMax, yPos + 1, area.YMax), NextRightId);
    }

    /// <summary>
    /// Splits this (not yet split) node into <see cref="Left"/> (left) and <see cref="Right"/>
    /// (right) children, cutting the area with a vertical line at <paramref name="xPos"/>.
    /// </summary>
    /// <param name="xPos">X-coordinate of the cut line; must lie strictly within the area.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="xPos"/> does not lie strictly within the area's XMin/XMax.
    /// </exception>
    /// <exception cref="InvalidOperationException">This node has already been split.</exception>
    internal void SplitVerticalAt(int xPos)
    {
        ValidateNoExistingSplit();

        if (xPos <= area.XMin || xPos >= area.XMax)
        {
            throw new ArgumentOutOfRangeException(nameof(xPos), "Out of bounds");
        }

        Left = new Node(new Area(area.XMin, xPos, area.YMin, area.YMax), NextLeftId);
        Right = new Node(new Area(xPos + 1, area.XMax, area.YMin, area.YMax), NextRightId);
    }

    void ValidateNoExistingSplit()
    {
        if (Left is not null || Right is not null)
        {
            throw new InvalidOperationException("Node is already split.");
        }
    }

    /// <summary>
    /// Returns the leaves of the tree rooted at this node. No particular order is guaranteed.
    /// </summary>
    /// <returns>An enumerable of leaf-nodes.</returns>
    internal IEnumerable<Node> Leaves()
    {
        if (Left is null)
        {
            yield return this;
            yield break;
        }
        foreach (var n in Left.Leaves())
            yield return n;
        foreach (var n in Right!.Leaves())
            yield return n;
    }

    /// <summary>
    /// Returns every node of the subtree rooted at this node, leaves and internal nodes alike.
    /// No particular order is guaranteed.
    /// </summary>
    internal IEnumerable<Node> AllNodes()
    {
        yield return this;
        if (Left is not null)
        {
            foreach (var n in Left.AllNodes())
                yield return n;
        }
        if (Right is not null)
        {
            foreach (var n in Right.AllNodes())
                yield return n;
        }
    }

    /// <summary>
    /// Recursively carves a randomly sized and positioned <see cref="Room"/> into each leaf of
    /// the subtree rooted at this node.
    /// </summary>
    /// <param name="minDistanceToDivider">
    /// Minimum gap kept between a leaf's carved room and its area's border (and so the divider
    /// lines/walls between sibling leaves), via <see cref="Area.CreateInnerAreaWithMargin"/>.
    /// </param>
    /// <param name="minWidth">Minimum width of a carved room.</param>
    /// <param name="minHeight">Minimum height of a carved room.</param>
    /// <param name="randomSource">
    /// Random source used to pick each room's size and position, and (if
    /// <paramref name="chanceOfLeafHavingNoRoom"/> is nonzero) whether a leaf gets a room at all.
    /// </param>
    /// <param name="chanceOfLeafHavingNoRoom">
    /// Raw chance [0,1] that a given leaf is skipped, leaving its <see cref="Room"/> null.
    /// </param>
    /// <param name="roomCarver">
    /// Strategy used to size and position each leaf's room within its (margin-adjusted) area.
    /// Defaults to <see cref="RectangularRoomCarver"/>. Inherited by descendants unless
    /// <paramref name="selectCarver"/> overrides it for a given node.
    /// </param>
    /// <param name="selectCarver">
    /// Optional per-node hook for varying the carver across the tree. Called for every node -
    /// leaf or internal - with the node itself and the carver inherited from its parent (the
    /// root's own <paramref name="roomCarver"/>, at the top of the recursion); returns the carver
    /// to use for that node and hand down to its children. Returning the carver unchanged keeps
    /// inheriting the current choice. Overriding it at an internal node makes the whole subtree
    /// beneath inherit the new carver (e.g. a cave-carved wing); overriding only at individual
    /// leaves instead gives independent per-room variation (e.g. the occasional two-rectangle
    /// room among rectangular ones).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="chanceOfLeafHavingNoRoom"/> is outside <c>[0,1]</c>, or a leaf's area
    /// cannot fit a room satisfying <paramref name="minWidth"/>/<paramref name="minHeight"/>
    /// after <paramref name="minDistanceToDivider"/> is applied.
    /// </exception>
    internal void CarveRooms(
        int minDistanceToDivider,
        int minWidth,
        int minHeight,
        Random randomSource,
        double chanceOfLeafHavingNoRoom = 0,
        IRoomCarver? roomCarver = null,
        Func<Node, IRoomCarver, Random, IRoomCarver>? selectCarver = null
    )
    {
        if (chanceOfLeafHavingNoRoom is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chanceOfLeafHavingNoRoom),
                chanceOfLeafHavingNoRoom,
                "must be a value between 0 and 1."
            );
        }

        var carver = roomCarver ?? RectangularRoomCarver.Instance;
        if (selectCarver is not null)
        {
            carver = selectCarver(this, carver, randomSource);
        }

        if (Left is null && Right is null)
        {
            // should we bail early, leaving this leaf with no room?
            if (randomSource.NextDouble() < chanceOfLeafHavingNoRoom)
            {
                return;
            }

            var innerAreaWithMargin = Area.CreateInnerAreaWithMargin(minDistanceToDivider);

            if (minWidth > innerAreaWithMargin.Width)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minWidth),
                    minWidth,
                    $"Leaf area {FormatArea(area)} only has {innerAreaWithMargin.Width} cells of "
                        + $"width after a margin of {minDistanceToDivider}; cannot fit a room this wide."
                );
            }
            if (minHeight > innerAreaWithMargin.Height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minHeight),
                    minHeight,
                    $"Leaf area {FormatArea(area)} only has {innerAreaWithMargin.Height} cells of "
                        + $"height after a margin of {minDistanceToDivider}; cannot fit a room this tall."
                );
            }

            Room = carver.CarveRoom(innerAreaWithMargin, minWidth, minHeight, randomSource);
            return;
        }

        Left?.CarveRooms(
            minDistanceToDivider,
            minWidth,
            minHeight,
            randomSource,
            chanceOfLeafHavingNoRoom,
            carver,
            selectCarver
        );
        Right?.CarveRooms(
            minDistanceToDivider,
            minWidth,
            minHeight,
            randomSource,
            chanceOfLeafHavingNoRoom,
            carver,
            selectCarver
        );
    }

    /// <summary>
    /// Bottom-up, as each split unwinds: connects <see cref="Left"/>'s and <see cref="Right"/>'s
    /// rooms/corridors with a new <see cref="Corridor"/>, so that every room reachable from this
    /// node ends up in one connected component. Call on the root once <see cref="CarveRooms"/>
    /// has run, to connect the whole plan.
    /// </summary>
    /// <param name="randomSource">
    /// Random source used to pick each corridor's elbow direction, and (when both children have a
    /// room) which child's point is carried up to represent this subtree to its parent.
    /// </param>
    /// <returns>
    /// A <see cref="GridPoint"/> reachable from every room in this subtree - the center of this
    /// node's <see cref="Room"/> for an unskipped leaf, or one end of the corridor just created
    /// for an internal node. <c>null</c> if this subtree contains no room at all (every leaf in
    /// it was skipped via <see cref="CarveRooms"/>'s <c>chanceOfLeafHavingNoRoom</c>) - callers
    /// must treat that as "nothing to connect to here" rather than assuming a room exists.
    /// </returns>
    internal GridPoint? ConnectRooms(Random randomSource)
    {
        if (Left is null && Right is null)
        {
            return Room is { } room ? new GridPoint(room.CenterX, room.CenterY) : null;
        }

        var leftPoint = Left!.ConnectRooms(randomSource);
        var rightPoint = Right!.ConnectRooms(randomSource);

        // One or both sides may have no room anywhere in their subtree - nothing to connect on
        // that side, so just pass the other side's point (or null) further up unconnected.
        if (leftPoint is null)
        {
            return rightPoint;
        }
        if (rightPoint is null)
        {
            return leftPoint;
        }

        Corridor = new Corridor(
            leftPoint.Value,
            rightPoint.Value,
            HorizontalFirst: randomSource.Next(0, 2) == 0
        );

        return randomSource.Next(0, 2) == 0 ? leftPoint : rightPoint;
    }

    /// <summary>
    /// Renders the subtree rooted at this node as an indented ASCII tree, for debugging.
    /// </summary>
    internal string ToTreeString()
    {
        var sb = new StringBuilder();
        _ = sb.Append("Root ")
            .Append(FormatArea(area))
            .Append("  {")
            .Append(Id)
            .Append('}')
            .Append('\n');
        AppendChildren(sb, "");
        return sb.ToString();
    }

    void AppendChildren(StringBuilder sb, string prefix)
    {
        var children = new List<(string Label, Node Node)>();
        if (Left is not null)
        {
            children.Add(("L", Left));
        }
        if (Right is not null)
        {
            children.Add(("R", Right));
        }

        for (int i = 0; i < children.Count; i++)
        {
            var (label, child) = children[i];
            bool isLast = i == children.Count - 1;
            _ = sb.Append(prefix)
                .Append(isLast ? "└── " : "├── ")
                .Append(label)
                .Append(' ')
                .Append(FormatArea(child.Area))
                .Append("  {")
                .Append(child.Id)
                .Append('}')
                .Append('\n');
            child.AppendChildren(sb, prefix + (isLast ? "    " : "│   "));
        }
    }

    static string FormatArea(Area area) =>
        $"[{area.XMin},{area.YMin}]-[{area.XMax},{area.YMax}] ({area.Width}x{area.Height})";

    const char DefaultAsciiDivider = ' ';
    const char DefaultAsciiCorridorFloor = '.';
    const char AsciiRoomFloor = '.';
    const char AsciiUncarved = ' ';

    /// <summary>
    /// Renders the plan under this node as an ASCII grid, for eyeballing room-carving and
    /// corridor-connection before the layout is transferred onto a real <see cref="Map"/>.
    ///
    /// Works on any node; the grid is sized and offset to this node's <see cref="Area"/>, so
    /// calling it on the root renders the whole map.
    /// </summary>
    internal string ToAsciiMap(
        char asciiDivider = DefaultAsciiDivider,
        char asciiCorridorFloor = DefaultAsciiCorridorFloor
    )
    {
        int originX = area.XMin;
        int originY = area.YMin;
        int width = area.Width;
        int height = area.Height;

        char[,] grid = new char[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                grid[y, x] = asciiDivider;
            }
        }

        foreach (var leaf in Leaves())
        {
            // Area coords are half-open [Min, Max); the cells a leaf owns are Min..Max-1. The
            // divider column/row a split consumes is owned by neither child, so it stays '#'.
            FillGrid(
                grid,
                leaf.Area.XMin - originX,
                leaf.Area.XMax - 1 - originX,
                leaf.Area.YMin - originY,
                leaf.Area.YMax - 1 - originY,
                AsciiUncarved
            );

            if (leaf.Room is { } room)
            {
                FillGrid(
                    grid,
                    room.Left - originX,
                    room.Right - originX,
                    room.Upper - originY,
                    room.Lower - originY,
                    AsciiRoomFloor
                );
            }
        }

        foreach (var node in AllNodes())
        {
            if (node.Corridor is not { } corridor)
            {
                continue;
            }

            foreach (var point in corridor.Points())
            {
                grid[point.Y - originY, point.X - originX] = asciiCorridorFloor;
            }
        }

        var sb = new StringBuilder((width + 1) * height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                _ = sb.Append(grid[y, x]);
            }
            _ = sb.Append('\n');
        }
        return sb.ToString();
    }

    static void FillGrid(char[,] grid, int xFrom, int xTo, int yFrom, int yTo, char value)
    {
        for (int y = yFrom; y <= yTo; y++)
        {
            for (int x = xFrom; x <= xTo; x++)
            {
                grid[y, x] = value;
            }
        }
    }

    public override string ToString() => ToTreeString();
}
