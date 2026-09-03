using System.Linq;
using BlazorRogue.World;

namespace BlazorRogue.Tests.World;

/// <summary>
/// Covers <see cref="LiquidEdging.Overlays"/> - the mapping from a liquid tile's land/liquid
/// neighbourhood to the composited <c>water_edging_*</c> shoreline pieces (image + rotation +
/// mirror). The common shapes resolve to a single hand-authored combo tile; only neighbourhoods no
/// single tile depicts fall back to stacking the atomic pieces.
/// </summary>
public class LiquidEdgingTests
{
    // Overlays(n, e, s, w, nw, ne, sw, se)
    static (string Image, int Rotation, bool Mirror)[] Overlays(
        bool n = false,
        bool e = false,
        bool s = false,
        bool w = false,
        bool nw = false,
        bool ne = false,
        bool sw = false,
        bool se = false
    ) => [.. LiquidEdging.Overlays(n, e, s, w, nw, ne, sw, se)];

    [Fact]
    public void OpenWaterOnEverySideProducesNoOverlays()
    {
        Assert.Empty(Overlays());
    }

    [Theory]
    [InlineData(false, false, false, true, 0)] // land west (canonical)
    [InlineData(true, false, false, false, 90)] // land north
    [InlineData(false, true, false, false, 180)] // land east
    [InlineData(false, false, true, false, 270)] // land south
    public void ASingleLandOrthogonalIsOneRotatedStraightEdge(
        bool n,
        bool e,
        bool s,
        bool w,
        int expectedRotation
    )
    {
        Assert.Equal(
            [("water_edging_1", expectedRotation, false)],
            Overlays(n: n, e: e, s: s, w: w)
        );
    }

    [Fact]
    public void TwoAdjacentLandOrthogonalsAreASingleConvexCornerTile()
    {
        Assert.Equal([("water_edging_2", 0, false)], Overlays(n: true, w: true));
        Assert.Equal([("water_edging_2", 90, false)], Overlays(n: true, e: true));
        Assert.Equal([("water_edging_2", 180, false)], Overlays(s: true, e: true));
        Assert.Equal([("water_edging_2", 270, false)], Overlays(s: true, w: true));
    }

    [Fact]
    public void TwoOppositeLandOrthogonalsAreASingleChannelTile()
    {
        Assert.Equal([("water_edging_3", 0, false)], Overlays(w: true, e: true));
        Assert.Equal([("water_edging_3", 90, false)], Overlays(n: true, s: true));
    }

    [Fact]
    public void AChannelWithSolidBanksIsStillTheSingleChannelTile()
    {
        // The diagonals being land (a straight bank) is a don't-care for the channel tile.
        Assert.Equal(
            [("water_edging_3", 0, false)],
            Overlays(w: true, e: true, nw: true, ne: true, sw: true, se: true)
        );
    }

    [Fact]
    public void ThreeLandSidesAreASingleThreeSidedTile()
    {
        Assert.Equal([("water_edging_4", 0, false)], Overlays(n: true, w: true, e: true));
    }

    [Fact]
    public void LandOnAllFourOrthogonalsIsTheSingleEnclosedTile()
    {
        Assert.Equal([("water_edging_8", 0, false)], Overlays(n: true, e: true, s: true, w: true));
    }

    [Fact]
    public void ALoneDiagonalLandIsASingleConcaveCornerTile()
    {
        Assert.Equal([("water_edging_9", 0, false)], Overlays(se: true));
        Assert.Equal([("water_edging_9", 90, false)], Overlays(sw: true));
        Assert.Equal([("water_edging_9", 180, false)], Overlays(nw: true));
        Assert.Equal([("water_edging_9", 270, false)], Overlays(ne: true));
    }

    [Fact]
    public void TwoAdjacentDiagonalNubsAreASingleTile()
    {
        Assert.Equal([("water_edging_10", 0, false)], Overlays(sw: true, se: true));
    }

    [Fact]
    public void ThreeDiagonalNubsAreASingleTile()
    {
        Assert.Equal([("water_edging_11", 0, false)], Overlays(sw: true, se: true, ne: true));
    }

    [Fact]
    public void TwoOppositeDiagonalNubsAreASingleTile()
    {
        Assert.Equal([("water_edging_12", 0, false)], Overlays(nw: true, se: true));
    }

    [Fact]
    public void AnEdgeWithADiagonalNubAtOneEndIsASingleTile()
    {
        // Canonical water_edging_5 is "land W + SW".
        Assert.Equal([("water_edging_5", 0, false)], Overlays(w: true, sw: true));
    }

    [Fact]
    public void TheMirrorImageOfEdgePlusNubUsesTheSameTileFlipped()
    {
        // "land W + NW" is water_edging_5's chiral partner - same art, mirrored.
        var overlays = Overlays(w: true, nw: true);

        Assert.Single(overlays);
        Assert.Equal("water_edging_5", overlays[0].Image);
        Assert.True(overlays[0].Mirror);
    }

    [Fact]
    public void AnEdgeWithNubsAtBothEndsIsASingleTile()
    {
        Assert.Equal([("water_edging_6", 0, false)], Overlays(w: true, nw: true, sw: true));
    }

    [Fact]
    public void AConvexCornerWithAFarNubIsASingleTile()
    {
        // Canonical water_edging_7 is "land W + N + SW".
        Assert.Equal([("water_edging_7", 0, false)], Overlays(n: true, w: true, sw: true));
    }

    [Fact]
    public void AnEdgeAndADetachedOppositeNubFallBackToTwoStackedAtomicPieces()
    {
        // No single tile depicts a north edge plus a nub at the far SE corner.
        var overlays = Overlays(n: true, se: true).OrderBy(o => o.Image).ToArray();

        Assert.Equal([("water_edging_1", 90, false), ("water_edging_9", 0, false)], overlays);
    }

    [Fact]
    public void EveryPossibleNeighbourhoodResolvesToAtMostAHandfulOfWellFormedSprites()
    {
        for (int mask = 0; mask < 256; mask++)
        {
            var overlays = Overlays(
                n: (mask & 1) != 0,
                e: (mask & 2) != 0,
                s: (mask & 4) != 0,
                w: (mask & 8) != 0,
                nw: (mask & 16) != 0,
                ne: (mask & 32) != 0,
                sw: (mask & 64) != 0,
                se: (mask & 128) != 0
            );
            // Worst case is a "plus" of open water with land in all four diagonals - four nubs.
            Assert.True(overlays.Length <= 4, $"mask {mask} produced {overlays.Length} sprites");
            Assert.All(overlays, o => Assert.True(o.Rotation is 0 or 90 or 180 or 270));
        }
    }

    [Fact]
    public void EveryContiguousShorelineShapeIsExactlyOneSprite()
    {
        // "Contiguous" = the land neighbours form one run around the tile (what you actually see
        // around an organic blob pool). Enumerated as: pick a start direction on the 8-ring and a
        // run length, mark that arc as land. Every such shape must resolve to a single combo tile.
        int[] ring = [0, 5, 1, 7, 2, 6, 3, 4]; // N, NE, E, SE, S, SW, W, NW  (clockwise)

        for (int start = 0; start < 8; start++)
        {
            for (int len = 1; len <= 7; len++)
            {
                int mask = 0;
                for (int k = 0; k < len; k++)
                {
                    mask |= 1 << ring[(start + k) % 8];
                }

                var overlays = Overlays(
                    n: (mask & 1) != 0,
                    e: (mask & 2) != 0,
                    s: (mask & 4) != 0,
                    w: (mask & 8) != 0,
                    nw: (mask & 16) != 0,
                    ne: (mask & 32) != 0,
                    sw: (mask & 64) != 0,
                    se: (mask & 128) != 0
                );

                Assert.True(
                    overlays.Length == 1,
                    $"contiguous shape (start {start}, len {len}, mask {mask}) gave {overlays.Length} sprites"
                );
            }
        }
    }
}
