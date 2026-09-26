using BlazorRogue.Entities;

namespace BlazorRogue.Tests;

/// <summary>
/// Covers StaticDecorativeObjectType.RandomImage/RandomImageWithAnimationClass - that every
/// configured image variant is reachable (regression coverage for an off-by-one in the random pick
/// that used to make random.Next(0, imageVariants.Count) exclude the last variant, e.g. rune_4 or
/// dust_6), and that a tag's image is never paired with a different tag's animation class (e.g.
/// lilypad's "a" species must never render with the "b" species' animation).
/// </summary>
public class StaticDecorativeObjectTypeTests
{
    static StaticDecorativeObjectType Type(
        Dictionary<string, string> image,
        Dictionary<string, string>? animationClasses = null
    ) =>
        new(
            id: "test",
            name: "Test",
            image: image,
            animationClasses: animationClasses ?? [],
            infoText: "Test",
            verticalOffset: 0,
            character: "",
            characterColor: "",
            blocking: false,
            makeCoveringOffsetDecsTransparent: false
        );

    [Fact]
    public void RandomImageEventuallyPicksEveryConfiguredVariant()
    {
        var images = new Dictionary<string, string>
        {
            ["0"] = "a",
            ["1"] = "b",
            ["2"] = "c",
        };
        var type = Type(images);

        var seen = new HashSet<string>();
        for (int i = 0; i < 200; i++)
        {
            _ = seen.Add(type.RandomImage);
        }

        Assert.Equal([.. images.Values], seen);
    }

    [Fact]
    public void RandomImageWithAnimationClassNeverMixesOneTagsImageWithAnotherTagsAnimation()
    {
        var type = Type(
            image: new Dictionary<string, string> { ["a"] = "img_a", ["b"] = "img_b" },
            animationClasses: new Dictionary<string, string> { ["a"] = "anim_a", ["b"] = "anim_b" }
        );

        var seenImages = new HashSet<string>();
        for (int i = 0; i < 200; i++)
        {
            var (image, animationClass) = type.RandomImageWithAnimationClass;
            _ = seenImages.Add(image);
            Assert.Equal(image == "img_a" ? "anim_a" : "anim_b", animationClass);
        }

        // Also confirms both tags actually got exercised across 200 draws, not just one.
        Assert.Equal(["img_a", "img_b"], seenImages);
    }

    [Fact]
    public void AnimationClassForTagIsNullWhenThatTagHasNoAnimationConfigured()
    {
        var type = Type(image: new Dictionary<string, string> { ["only"] = "img" });

        Assert.Null(type.AnimationClassForTag("only"));
    }
}
