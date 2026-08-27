using System;
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

        Assert.Equal("Root [0,0]-[20,10] (20x10)\n", node.ToTreeString());
    }

    [Fact]
    public void ToTreeStringRendersSplitsAsIndentedChildren()
    {
        var node = new Node(new Area(0, 20, 0, 10));
        node.SplitVerticalAt(9);
        node.Left!.SplitHorizontalAt(4);

        var expected =
            "Root [0,0]-[20,10] (20x10)\n"
            + "├── L [0,0]-[9,10] (9x10)\n"
            + "│   ├── L [0,0]-[9,4] (9x4)\n"
            + "│   └── R [0,5]-[9,10] (9x5)\n"
            + "└── R [10,0]-[20,10] (10x10)\n";

        Assert.Equal(expected, node.ToTreeString());
    }

    // Not a real test - run with
    // `dotnet test --filter DisplayName~Print --logger "console;verbosity=detailed"` to eyeball
    // the tree output for a random split, e.g. while tweaking SplitUntilThreshold's behavior.
    [Fact]
    public void PrintRandomSplitForManualInspection()
    {
        var node = new Node(new Area(0, 60, 0, 40));
        var leaves = new List<Node>();
        node.SplitUntilThreshold(25, 10, new Random(1), leaves);

        output.WriteLine(node.ToTreeString());
        output.WriteLine($"no of leaves: {leaves.Count}");

        var ids = leaves.Select(l => l.Id);
        string idString = string.Join(", ", ids);

        output.WriteLine($"ids: [{idString}]");
    }
}
