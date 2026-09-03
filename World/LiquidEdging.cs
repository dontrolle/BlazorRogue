using System.Collections.Generic;

namespace BlazorRogue.World;

/// <summary>
/// Pure mapping from "which of a liquid tile's 8 neighbours are land" to the set of
/// <c>water_edging_*</c> overlay sprites (and their clockwise rotation) that draw the shoreline.
/// Kept separate from <see cref="Tile"/> so it can be unit-tested in isolation.
/// </summary>
/// <remarks>
/// Only the atomic pieces are used: <c>water_edging_1</c> (a straight edge, canonically land-to-west),
/// <c>water_edging_2</c> (a convex corner, canonically land north+west, which already includes the
/// edge treatment along both of its sides), <c>water_edging_9</c> (a concave/inner corner,
/// canonically land at the south-east diagonal) and <c>water_edging_8</c> (a fully enclosed single
/// tile). A straight edge is only emitted for a side that no convex corner already covers, so a
/// plain shoreline tile gets exactly one sprite; a few patterns (three land sides, two opposite
/// land sides, an edge plus a detached diagonal nub) still stack two until the hand-authored combo
/// tiles 3-7 and 10-12 land.
/// </remarks>
static class LiquidEdging
{
    /// <remarks>
    /// Each argument is true when that neighbour is land - a non-liquid tile, or off the map edge.
    /// </remarks>
    public static IEnumerable<(string Image, int Rotation)> Overlays(
        bool n,
        bool e,
        bool s,
        bool w,
        bool nw,
        bool ne,
        bool sw,
        bool se
    )
    {
        // Fully surrounded on all four sides - a one-tile pool - has its own dedicated sprite.
        if (n && e && s && w)
        {
            yield return ("water_edging_8", 0);
            yield break;
        }

        // Straight edge for a land side that isn't already covered by a convex corner (i.e. both of
        // its flanking orthogonals are liquid). Canonical tile faces west.
        if (w && !n && !s)
        {
            yield return ("water_edging_1", 0);
        }
        if (n && !w && !e)
        {
            yield return ("water_edging_1", 90);
        }
        if (e && !n && !s)
        {
            yield return ("water_edging_1", 180);
        }
        if (s && !w && !e)
        {
            yield return ("water_edging_1", 270);
        }

        // Convex corners where two adjacent orthogonal neighbours are both land (canonical: N+W).
        if (n && w)
        {
            yield return ("water_edging_2", 0);
        }
        if (n && e)
        {
            yield return ("water_edging_2", 90);
        }
        if (s && e)
        {
            yield return ("water_edging_2", 180);
        }
        if (s && w)
        {
            yield return ("water_edging_2", 270);
        }

        // Concave corners: a lone land tile touching only at a diagonal, both flanking orthogonals
        // still liquid (canonical: land at SE).
        if (se && !s && !e)
        {
            yield return ("water_edging_9", 0);
        }
        if (sw && !s && !w)
        {
            yield return ("water_edging_9", 90);
        }
        if (nw && !n && !w)
        {
            yield return ("water_edging_9", 180);
        }
        if (ne && !n && !e)
        {
            yield return ("water_edging_9", 270);
        }
    }
}
