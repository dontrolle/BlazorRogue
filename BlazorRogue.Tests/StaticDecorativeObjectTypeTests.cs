using System.Collections.Generic;
using System.Linq;
using BlazorRogue.Entities;

namespace BlazorRogue.Tests;

/// <summary>
/// Covers StaticDecorativeObjectType.RandomImage - specifically that every configured image
/// variant is reachable (regression coverage for an off-by-one in the random pick that used to
/// make random.Next(0, imageVariants.Count) exclude the last variant, e.g. rune_4 or dust_6).
/// </summary>
public class StaticDecorativeObjectTypeTests
{
    [Fact]
    public void RandomImageEventuallyPicksEveryConfiguredVariant()
    {
        var images = new Dictionary<string, string>
        {
            ["0"] = "a",
            ["1"] = "b",
            ["2"] = "c",
        };
        var type = new StaticDecorativeObjectType(
            id: "test",
            name: "Test",
            image: images,
            infoText: "Test",
            verticalOffset: 0,
            character: "",
            characterColor: "",
            blocking: false,
            makeCoveringOffsetDecsTransparent: false
        );

        var seen = new HashSet<string>();
        for (int i = 0; i < 200; i++)
        {
            seen.Add(type.RandomImage);
        }

        Assert.Equal(images.Values.ToHashSet(), seen);
    }
}
