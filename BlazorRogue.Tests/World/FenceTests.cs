using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue.Tests.World;

/// <summary>
/// Covers the fence decoration types in Data/decorations.json (dontrolle/BlazorRogue-internal#86,
/// Stage 1): each is a plain StaticDecorativeObjectType riding the shared "fence" Edge-blocking
/// primitive (see Edge/GameObject.BlockedEdges) that Statue already proves out - these tests confirm
/// decorations.json parses with the intended image/Blocking/BlockedEdges, and that a full
/// rectangular enclosure built from them contains its interior except at a gate. No map-generation
/// placement exists yet - these types are only reachable by direct placement, as here.
/// </summary>
public class FenceTests
{
    static Map CreateMap(int width = 10, int height = 10)
    {
        var wallSet = new TileSet("test_wall", TileType.Wall, "test", [0]);
        return new Map(width, height, wallSet, game: null!);
    }

    // Constructing a Game parses the real Data/*.json and populates References.Configuration (see
    // Game(), Game.cs) - the same mechanism StatuePlacementTests relies on via map generation.
    static StaticDecorativeObjectType FenceType(string id)
    {
        _ = new Game();
        return References.Configuration.StaticDecorativeObjectTypes[id];
    }

    // (expectedBlocksNorth, expectedBlocksEast, expectedBlocksSouth, expectedBlocksWest) - avoids
    // the internal Edge enum in this public Theory's signature; combined into an Edge inside the
    // method body, which is fine since that's not part of the public signature.
    [Theory]
    [InlineData("fence_straight", "fence_13", false, false, false, true, false)]
    [InlineData("fence_end_west", "fence_2", false, false, false, true, false)]
    [InlineData("fence_end_east", "fence_4", false, false, false, true, false)]
    [InlineData("fence_opening", "fence_3", false, false, false, false, false)]
    [InlineData("fence_wall_west", "fence_7", false, false, false, false, true)]
    [InlineData("fence_wall_east", "fence_6", false, false, true, false, false)]
    [InlineData("fence_corner_sw", "fence_12", false, false, false, true, true)]
    [InlineData("fence_corner_se", "fence_14", false, false, true, true, false)]
    [InlineData("fence_tjunction", "fence_10", false, false, true, true, true)]
    [InlineData("fence_pillar_west", "fence_8", true, false, false, false, false)]
    [InlineData("fence_pillar_east", "fence_9", true, false, false, false, false)]
    public void FenceTypesParseWithExpectedImageBlockingAndBlockedEdges(
        string id,
        string expectedImage,
        bool expectedBlocking,
        bool expectedBlocksNorth,
        bool expectedBlocksEast,
        bool expectedBlocksSouth,
        bool expectedBlocksWest
    )
    {
        var sdot = FenceType(id);

        var expectedBlockedEdges = Edge.None;
        if (expectedBlocksNorth)
        {
            expectedBlockedEdges |= Edge.North;
        }
        if (expectedBlocksEast)
        {
            expectedBlockedEdges |= Edge.East;
        }
        if (expectedBlocksSouth)
        {
            expectedBlockedEdges |= Edge.South;
        }
        if (expectedBlocksWest)
        {
            expectedBlockedEdges |= Edge.West;
        }

        Assert.Equal(expectedImage, sdot.RandomImage);
        Assert.Equal(expectedBlocking, sdot.Blocking);
        Assert.Equal(expectedBlockedEdges, sdot.BlockedEdges);
    }

    // Builds the same 3x3 enclosure (north wall with a center gate, west/east side walls, south
    // wall) validated via the composite mockups in issue #86: a west-cap/opening/east-cap north row,
    // vertical west/east runs, and a corner/straight/corner south row.
    [Fact]
    public void RectangularEnclosureContainsItsInteriorExceptThroughTheGate()
    {
        var map = CreateMap();

        void Place(int x, int y, string typeId) =>
            map.AddGameObject(new StaticDecorativeObject(x, y, FenceType(typeId)));

        Place(1, 1, "fence_end_west");
        Place(2, 1, "fence_opening"); // gate
        Place(3, 1, "fence_end_east");
        Place(1, 2, "fence_wall_west");
        Place(3, 2, "fence_wall_east");
        Place(1, 3, "fence_corner_sw");
        Place(2, 3, "fence_straight");
        Place(3, 3, "fence_corner_se");

        // The gate is a clear passage straight through, outside to interior.
        Assert.False(map.IsMovementBlockedAcrossEdge(2, 0, 2, 1));
        Assert.False(map.IsMovementBlockedAcrossEdge(2, 1, 2, 2));

        // Every other perimeter edge keeps the true outside out...
        Assert.True(map.IsMovementBlockedAcrossEdge(1, 2, 0, 2)); // west wall
        Assert.True(map.IsMovementBlockedAcrossEdge(3, 2, 4, 2)); // east wall
        Assert.True(map.IsMovementBlockedAcrossEdge(1, 3, 1, 4)); // south wall (sw corner)
        Assert.True(map.IsMovementBlockedAcrossEdge(2, 3, 2, 4)); // south wall (straight)
        Assert.True(map.IsMovementBlockedAcrossEdge(3, 3, 3, 4)); // south wall (se corner)
        Assert.True(map.IsMovementBlockedAcrossEdge(1, 3, 0, 3)); // sw corner, west side
        Assert.True(map.IsMovementBlockedAcrossEdge(3, 3, 4, 3)); // se corner, east side

        // ...and the interior is fully open.
        Assert.False(map.IsMovementBlockedAcrossEdge(2, 2, 1, 2));
        Assert.False(map.IsMovementBlockedAcrossEdge(2, 2, 3, 2));
        Assert.False(map.IsMovementBlockedAcrossEdge(2, 2, 2, 1));
        Assert.False(map.IsMovementBlockedAcrossEdge(2, 2, 2, 3));
    }

    [Fact]
    public void FreestandingPillarIsTwoIndependentBlockingHalvesWithNoEdgeBlocking()
    {
        var map = CreateMap();
        var west = new StaticDecorativeObject(5, 5, FenceType("fence_pillar_west"));
        var east = new StaticDecorativeObject(6, 5, FenceType("fence_pillar_east"));
        map.AddGameObject(west);
        map.AddGameObject(east);
        west.Render(map);
        east.Render(map);

        Assert.True(west.Blocking);
        Assert.True(east.Blocking);
        Assert.Equal(Edge.None, west.BlockedEdges);
        Assert.Equal(Edge.None, east.BlockedEdges);

        Assert.Contains(map.Decorations[5, 5], d => d.ImageName == "fence_8");
        Assert.Contains(map.Decorations[6, 5], d => d.ImageName == "fence_9");

        // No edge-blocking between the two halves or to their neighbours - Blocking (tile
        // occupancy) is what keeps a mover off each post, not BlockedEdges.
        Assert.False(map.IsMovementBlockedAcrossEdge(5, 5, 6, 5));
    }
}
