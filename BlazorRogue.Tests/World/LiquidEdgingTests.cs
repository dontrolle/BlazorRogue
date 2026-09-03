using System.Linq;
using BlazorRogue.World;

namespace BlazorRogue.Tests.World;

/// <summary>
/// Covers <see cref="LiquidEdging.Overlays"/> - the pure mapping from a liquid tile's land/liquid
/// neighbourhood to the composited <c>water_edging_*</c> shoreline pieces and their rotation.
/// </summary>
public class LiquidEdgingTests
{
    // Overlays(n, e, s, w, nw, ne, sw, se)

    static (string Image, int Rotation)[] Overlays(
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
    [InlineData(true, false, false, false, 90)] // land to the north
    [InlineData(false, true, false, false, 180)] // land to the east
    [InlineData(false, false, true, false, 270)] // land to the south
    [InlineData(false, false, false, true, 0)] // land to the west (canonical)
    public void ASingleLandOrthogonalProducesOneRotatedStraightEdge(
        bool n,
        bool e,
        bool s,
        bool w,
        int expectedRotation
    )
    {
        var overlays = Overlays(n: n, e: e, s: s, w: w);

        Assert.Contains(("water_edging_1", expectedRotation), overlays);
        Assert.DoesNotContain(overlays, o => o.Image == "water_edging_2");
    }

    [Fact]
    public void TwoAdjacentLandOrthogonalsAreJustTheConvexCornerNotAlsoTwoStraightEdges()
    {
        // The convex-corner sprite already carries the edge treatment along both its sides, so the
        // straight edges for N and W are suppressed - one sprite, not three.
        var overlays = Overlays(n: true, w: true);

        Assert.Equal([("water_edging_2", 0)], overlays);
    }

    [Fact]
    public void AStraightEdgeIsStillDrawnForASideWithNoCornerAtEitherEnd()
    {
        // Land to the north and south (a channel): both are straight edges, no corner suppresses
        // them, and they don't overlap.
        var overlays = Overlays(n: true, s: true);

        Assert.Contains(("water_edging_1", 90), overlays);
        Assert.Contains(("water_edging_1", 270), overlays);
        Assert.DoesNotContain(overlays, o => o.Image == "water_edging_2");
    }

    [Fact]
    public void ThreeLandSidesAreTwoConvexCornersWithNoStraightEdges()
    {
        var overlays = Overlays(n: true, w: true, e: true);

        Assert.Equal(
            [("water_edging_2", 0), ("water_edging_2", 90)],
            overlays.OrderBy(o => o.Rotation).ToArray()
        );
    }

    [Fact]
    public void ConvexCornersRotateWithTheirOrientation()
    {
        Assert.Contains(("water_edging_2", 90), Overlays(n: true, e: true));
        Assert.Contains(("water_edging_2", 180), Overlays(s: true, e: true));
        Assert.Contains(("water_edging_2", 270), Overlays(s: true, w: true));
    }

    [Fact]
    public void LandOnAllFourOrthogonalsIsTheSingleEnclosedTileOnly()
    {
        var overlays = Overlays(n: true, e: true, s: true, w: true);

        Assert.Equal([("water_edging_8", 0)], overlays);
    }

    [Fact]
    public void ALoneDiagonalLandInOpenWaterIsAConcaveCorner()
    {
        // SE diagonal is land, both flanking orthogonals (S, E) still liquid -> canonical inner corner.
        var overlays = Overlays(se: true);

        Assert.Equal([("water_edging_9", 0)], overlays);
    }

    [Fact]
    public void ConcaveCornersRotateWithTheirOrientation()
    {
        Assert.Contains(("water_edging_9", 90), Overlays(sw: true));
        Assert.Contains(("water_edging_9", 180), Overlays(nw: true));
        Assert.Contains(("water_edging_9", 270), Overlays(ne: true));
    }

    [Fact]
    public void ADiagonalIsNotAConcaveCornerWhenAFlankingOrthogonalIsAlreadyLand()
    {
        // S is land, so the SE diagonal is already covered by the south straight edge - no nub.
        var overlays = Overlays(s: true, se: true);

        Assert.DoesNotContain(overlays, o => o.Image == "water_edging_9");
        Assert.Contains(("water_edging_1", 270), overlays);
    }
}
