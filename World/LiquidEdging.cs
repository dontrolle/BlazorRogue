using System.Collections.Generic;
using System.Numerics;

namespace BlazorRogue.World;

/// <summary>
/// Pure mapping from "which of a liquid tile's 8 neighbours are land" to the <c>water_edging_*</c>
/// overlay sprites (image + clockwise rotation + optional horizontal mirror) that draw the shoreline.
/// Kept separate from <see cref="Tile"/> so it can be unit-tested in isolation.
/// </summary>
/// <remarks>
/// Each of the 12 <c>water_edging_*</c> tiles depicts one exact neighbourhood shape (in a canonical
/// orientation). For a given tile we look for the single tile whose shape - rotated/mirrored - is an
/// exact match, so the common cases (straight shore, outside corner, three land sides, a lone
/// diagonal nub, an enclosed tile, ...) are one sprite. Only neighbourhoods no single tile depicts
/// (e.g. an edge plus a detached diagonal nub) fall back to stacking the atomic pieces
/// <c>water_edging_1/2/9</c>.
/// </remarks>
static class LiquidEdging
{
    // Neighbour bit indices.
    const int Nn = 0;
    const int Ee = 1;
    const int Ss = 2;
    const int Ww = 3;
    const int NWi = 4;
    const int NEi = 5;
    const int SWi = 6;
    const int SEi = 7;

    static int Mask(params int[] dirs)
    {
        int m = 0;
        foreach (int d in dirs)
        {
            m |= 1 << d;
        }
        return m;
    }

    // Canonical land-direction set for each water_edging_* tile. 1/2/8/9 double as the atomic
    // straight-edge / convex-corner / enclosed / concave-corner pieces; 3-7 and 10-12 are the
    // hand-authored combos.
    static readonly (string Image, int Land)[] Combos =
    [
        ("water_edging_1", Mask(Ww)),
        ("water_edging_2", Mask(Nn, Ww)),
        ("water_edging_3", Mask(Ww, Ee)),
        ("water_edging_4", Mask(Ww, Nn, Ee)),
        ("water_edging_5", Mask(Ww, SWi)),
        ("water_edging_6", Mask(Ww, NWi, SWi)),
        ("water_edging_7", Mask(Ww, Nn, SWi)),
        ("water_edging_8", Mask(Nn, Ee, Ss, Ww)),
        ("water_edging_9", Mask(SEi)),
        ("water_edging_10", Mask(SWi, SEi)),
        ("water_edging_11", Mask(SWi, SEi, NEi)),
        ("water_edging_12", Mask(NWi, SEi)),
    ];

    // Each diagonal's two flanking orthogonals, indexed diag-4 (NW, NE, SW, SE).
    static readonly (int A, int B)[] DiagFlanks = [(Nn, Ww), (Nn, Ee), (Ss, Ww), (Ss, Ee)];

    /// <summary>
    /// (mustBeLand, mustBeLiquid) signature for a combo tile's canonical land set: every orthogonal
    /// not in the set must be liquid; a diagonal not in the set is pinned to liquid only when both
    /// its flanking orthogonals are water too (where a land diagonal would be a concave nub this
    /// tile doesn't draw) - if either flank is land the diagonal sits behind that edge and is a
    /// don't-care.
    /// </summary>
    static (int Land, int Liquid) Signature(int land)
    {
        int liquid = 0;
        for (int d = Nn; d <= Ww; d++)
        {
            if ((land & (1 << d)) == 0)
            {
                liquid |= 1 << d;
            }
        }
        for (int diag = NWi; diag <= SEi; diag++)
        {
            if ((land & (1 << diag)) != 0)
            {
                continue;
            }
            var (a, b) = DiagFlanks[diag - NWi];
            bool eitherFlankLand = (land & (1 << a)) != 0 || (land & (1 << b)) != 0;
            if (!eitherFlankLand)
            {
                liquid |= 1 << diag;
            }
        }
        return (land, liquid);
    }

    // Clockwise 90 degrees: N->E->S->W->N, NW->NE->SE->SW->NW.
    static int Rotate90(int mask)
    {
        int r = 0;
        if ((mask & (1 << Nn)) != 0)
        {
            r |= 1 << Ee;
        }
        if ((mask & (1 << Ee)) != 0)
        {
            r |= 1 << Ss;
        }
        if ((mask & (1 << Ss)) != 0)
        {
            r |= 1 << Ww;
        }
        if ((mask & (1 << Ww)) != 0)
        {
            r |= 1 << Nn;
        }
        if ((mask & (1 << NWi)) != 0)
        {
            r |= 1 << NEi;
        }
        if ((mask & (1 << NEi)) != 0)
        {
            r |= 1 << SEi;
        }
        if ((mask & (1 << SEi)) != 0)
        {
            r |= 1 << SWi;
        }
        if ((mask & (1 << SWi)) != 0)
        {
            r |= 1 << NWi;
        }
        return r;
    }

    // Horizontal flip: W<->E, NW<->NE, SW<->SE; N and S unchanged.
    static int MirrorX(int mask)
    {
        int r = mask & ((1 << Nn) | (1 << Ss));
        if ((mask & (1 << Ww)) != 0)
        {
            r |= 1 << Ee;
        }
        if ((mask & (1 << Ee)) != 0)
        {
            r |= 1 << Ww;
        }
        if ((mask & (1 << NWi)) != 0)
        {
            r |= 1 << NEi;
        }
        if ((mask & (1 << NEi)) != 0)
        {
            r |= 1 << NWi;
        }
        if ((mask & (1 << SWi)) != 0)
        {
            r |= 1 << SEi;
        }
        if ((mask & (1 << SEi)) != 0)
        {
            r |= 1 << SWi;
        }
        return r;
    }

    // Precomputed overlay list for every possible 8-neighbour land bitmask (0..255).
    static readonly (string Image, int Rotation, bool Mirror)[][] Table = BuildTable();

    static (string Image, int Rotation, bool Mirror)[][] BuildTable()
    {
        var table = new (string, int, bool)[256][];
        for (int mask = 0; mask < 256; mask++)
        {
            table[mask] = ExactCombo(mask) is { } combo ? [combo] : [.. AtomicComposite(mask)];
        }
        return table;
    }

    // The best exact match for a neighbourhood, or null if no single tile depicts it. "Best" = the
    // tile that accounts for the most land neighbours (an outside corner beats a straight edge that
    // happens to ignore a diagonal nub; the enclosed tile beats everything).
    static (string Image, int Rotation, bool Mirror)? ExactCombo(int mask)
    {
        (string Image, int Rotation, bool Mirror)? best = null;
        int bestLandCount = -1;

        foreach (var (image, canonLand) in Combos)
        {
            int landCount = BitOperations.PopCount((uint)canonLand);
            if (landCount <= bestLandCount)
            {
                continue;
            }

            var (sigLand, sigLiquid) = Signature(canonLand);
            if (TryMatch(mask, sigLand, sigLiquid, out int rotation, out bool mirror))
            {
                best = (image, rotation, mirror);
                bestLandCount = landCount;
            }
        }

        return best;
    }

    static bool TryMatch(int mask, int sigLand, int sigLiquid, out int rotation, out bool mirror)
    {
        for (int m = 0; m < 2; m++)
        {
            int land = m == 1 ? MirrorX(sigLand) : sigLand;
            int liquid = m == 1 ? MirrorX(sigLiquid) : sigLiquid;
            for (int rot = 0; rot < 360; rot += 90)
            {
                if ((mask & land) == land && (mask & liquid) == 0)
                {
                    rotation = rot;
                    mirror = m == 1;
                    return true;
                }
                land = Rotate90(land);
                liquid = Rotate90(liquid);
            }
        }

        rotation = 0;
        mirror = false;
        return false;
    }

    // Stack the atomic pieces for a neighbourhood no single tile depicts: a straight edge per land
    // side no convex corner already covers, a convex corner per adjacent land pair, and a concave
    // nub per land diagonal whose flanking orthogonals are both liquid.
    static IEnumerable<(string Image, int Rotation, bool Mirror)> AtomicComposite(int mask)
    {
        bool L(int d) => (mask & (1 << d)) != 0;
        bool n = L(Nn),
            e = L(Ee),
            s = L(Ss),
            w = L(Ww),
            nw = L(NWi),
            ne = L(NEi),
            sw = L(SWi),
            se = L(SEi);

        if (w && !n && !s)
        {
            yield return ("water_edging_1", 0, false);
        }
        if (n && !w && !e)
        {
            yield return ("water_edging_1", 90, false);
        }
        if (e && !n && !s)
        {
            yield return ("water_edging_1", 180, false);
        }
        if (s && !w && !e)
        {
            yield return ("water_edging_1", 270, false);
        }

        if (n && w)
        {
            yield return ("water_edging_2", 0, false);
        }
        if (n && e)
        {
            yield return ("water_edging_2", 90, false);
        }
        if (s && e)
        {
            yield return ("water_edging_2", 180, false);
        }
        if (s && w)
        {
            yield return ("water_edging_2", 270, false);
        }

        if (se && !s && !e)
        {
            yield return ("water_edging_9", 0, false);
        }
        if (sw && !s && !w)
        {
            yield return ("water_edging_9", 90, false);
        }
        if (nw && !n && !w)
        {
            yield return ("water_edging_9", 180, false);
        }
        if (ne && !n && !e)
        {
            yield return ("water_edging_9", 270, false);
        }
    }

    /// <remarks>
    /// Each argument is true when that neighbour is land - a non-liquid tile, or off the map edge.
    /// </remarks>
    public static IReadOnlyList<(string Image, int Rotation, bool Mirror)> Overlays(
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
        int mask = 0;
        if (n)
        {
            mask |= 1 << Nn;
        }
        if (e)
        {
            mask |= 1 << Ee;
        }
        if (s)
        {
            mask |= 1 << Ss;
        }
        if (w)
        {
            mask |= 1 << Ww;
        }
        if (nw)
        {
            mask |= 1 << NWi;
        }
        if (ne)
        {
            mask |= 1 << NEi;
        }
        if (sw)
        {
            mask |= 1 << SWi;
        }
        if (se)
        {
            mask |= 1 << SEi;
        }
        return Table[mask];
    }
}
