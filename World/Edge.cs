using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorRogue.World;

/// <summary>
/// Which edge(s) of its own tile a GameObject blocks movement across - a "fence" primitive,
/// independent of whether the tile itself is Blocking for occupancy. E.g. Statue sets North to
/// block stepping between its tile and the tile directly north of it (where its "top" half is
/// drawn), while the tile stays enterable from every other side. See GameObject.BlockedEdges and
/// Map.IsMovementBlockedAcrossEdge.
/// </summary>
[Flags]
enum Edge
{
    None = 0,
    North = 1,
    South = 2,
    East = 4,
    West = 8,
}

static class EdgeExtensions
{
    /// <summary>
    /// The individual single-edge flags set in a (possibly combined) <see cref="Edge"/> value -
    /// e.g. so GamePage.razor can draw one ASCII-mode border marker per blocked edge.
    /// </summary>
    public static IEnumerable<Edge> SetFlags(this Edge edges) =>
        Enum.GetValues<Edge>().Where(e => e != Edge.None && edges.HasFlag(e));
}
