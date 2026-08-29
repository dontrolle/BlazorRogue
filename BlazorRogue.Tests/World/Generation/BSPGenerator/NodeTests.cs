using BlazorRogue.World.Generation;
using BlazorRogue.World.Generation.BSPGenerator;
using Xunit.Abstractions;

namespace BlazorRogue.Tests.World.Generation.BSPGenerator;

public class NodeTests(ITestOutputHelper output)
{
    [Fact]
    public void ToTreeStringRendersUnsplitNodeAsRootOnly()
    {
        var node = new Node(new Area(0, 20, 0, 10));

        Assert.Equal("Root [0,0]-[20,10] (20x10)  {0}\n", node.ToTreeString());
    }

    [Fact]
    public void LeavesOfAnUnsplitNodeIsJustItself()
    {
        var node = new Node(new Area(0, 20, 0, 10));

        Assert.Equal([node], node.Leaves());
    }

    [Fact]
    public void ToTreeStringRendersSplitsAsIndentedChildren()
    {
        var node = new Node(new Area(0, 20, 0, 10));
        node.SplitVerticalAt(9);
        node.Left!.SplitHorizontalAt(4);

        var expectedTree =
            "Root [0,0]-[20,10] (20x10)  {0}\n"
            + "├── L [0,0]-[9,10] (9x10)  {1}\n"
            + "│   ├── L [0,0]-[9,4] (9x4)  {3}\n"
            + "│   └── R [0,5]-[9,10] (9x5)  {4}\n"
            + "└── R [10,0]-[20,10] (10x10)  {2}\n";

        Assert.Equal(expectedTree, node.ToTreeString());
    }

    [Fact]
    public void LeavesReturnsAllLeafNodesOfASplitTree()
    {
        var node = new Node(new Area(0, 20, 0, 10));
        node.SplitVerticalAt(9);
        node.Left!.SplitHorizontalAt(4);

        IEnumerable<int> expectedLeafIds = [2, 3, 4];
        Assert.Equal(expectedLeafIds, node.Leaves().Select(n => n.Id).OrderBy(i => i));
    }

    [Fact]
    public void SelectCarverOverrideAtAnInternalNodePropagatesToTheWholeSubtree()
    {
        // Root splits into Left (a further-split internal node with two leaves) and Right (a
        // single leaf). Overriding the carver at Left should apply to both of its leaves, while
        // Right - outside that subtree - keeps inheriting the default.
        var root = new Node(new Area(0, 20, 0, 16));
        root.SplitVerticalAt(9);
        root.Left!.SplitHorizontalAt(7);

        Func<Node, IRoomCarver, Random, IRoomCarver> selectCarver = (node, inherited, _) =>
            node == root.Left ? OverlaidRectanglesRoomCarver.Instance : inherited;

        root.CarveRooms(0, 3, 3, new Random(1), selectCarver: selectCarver);

        Assert.All(root.Left!.Leaves(), leaf => Assert.Equal(RoomType.Overlaid, leaf.Room!.Type));
        Assert.All(
            root.Right!.Leaves(),
            leaf => Assert.Equal(RoomType.Rectangular, leaf.Room!.Type)
        );
    }

    // Not a real test - run with
    // `dotnet test --filter DisplayName~PrintRandomSplit --logger "console;verbosity=detailed"` to
    // eyeball the tree output for a random split, e.g. while tweaking SplitUntilThreshold's
    // behavior. (Filtering on ~Print alone would also run BspLayoutTests' inspection test.)
    [Fact]
    public void PrintRandomSplitForManualInspection()
    {
        var node = new Node(new Area(0, 60, 0, 40));
        node.SplitUntilThreshold(25, 10, new Random(1));

        output.WriteLine(node.ToTreeString());

        var leaves = node.Leaves();
        output.WriteLine($"no of leaves: {leaves.Count()}");

        var ids = leaves.Select(l => l.Id);
        string idString = string.Join(", ", ids);

        output.WriteLine($"ids: [{idString}]");
    }
}
