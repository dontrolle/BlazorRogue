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
    internal int Id => id;
    // private Room? room;
    // private Corridor? corridor;
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
    /// Random source used to pick the split axis (when both are viable) and the split position.
    /// </param>
    /// <param name="leafNodes">List of seen leaf-nodes in the BSP tree.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="threshold"/> is less than <c>2 * minSplit</c>.
    /// </exception>
    internal void SplitUntilThreshold(int threshold, int minSplit, Random randomSource, List<Node> leafNodes)
    {
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
        if (!mayHorizontalSplit && !mayVerticalSplit)
        {
            // we are at a leaf
            leafNodes.Add(this);
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
            int yPos = randomSource.Next(area.YMin + minSplit, area.YMax - minSplit);
            SplitHorizontalAt(yPos);
        }
        else
        {
            int xPos = randomSource.Next(area.XMin + minSplit, area.XMax - minSplit);
            SplitVerticalAt(xPos);
        }

        // and now recurse
        Left!.SplitUntilThreshold(threshold, minSplit, randomSource, leafNodes);
        Right!.SplitUntilThreshold(threshold, minSplit, randomSource, leafNodes);
    }

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

        Left = new Node(new Area(area.XMin, area.XMax, area.YMin, yPos), (Id * 2) + 1);
        Right = new Node(new Area(area.XMin, area.XMax, yPos + 1, area.YMax), (Id * 2) + 2);
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

        Left = new Node(new Area(area.XMin, xPos, area.YMin, area.YMax), (Id * 2) + 1);
        Right = new Node(new Area(xPos + 1, area.XMax, area.YMin, area.YMax), (Id * 2) + 2);
    }

    void ValidateNoExistingSplit()
    {
        if (Left is not null || Right is not null)
        {
            throw new InvalidOperationException("Node is already split.");
        }
    }

    /// <summary>
    /// Renders the subtree rooted at this node as an indented ASCII tree, for debugging.
    /// </summary>
    internal string ToTreeString()
    {
        var sb = new StringBuilder();
        _ = sb.Append("Root ").Append(FormatArea(area)).Append("  {").Append(Id).Append('}').Append('\n');
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

    public override string ToString() => ToTreeString();
}
