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
        IEnumerable<int> expectedLeafIds = [2, 3, 4];
        Assert.Equal(expectedLeafIds, node.Leaves().Select(n => n.Id).OrderBy(i => i));
    }

    // Not a real test - run with
    // `dotnet test --filter DisplayName~Print --logger "console;verbosity=detailed"` to eyeball
    // the tree output for a random split, e.g. while tweaking SplitUntilThreshold's behavior.
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
