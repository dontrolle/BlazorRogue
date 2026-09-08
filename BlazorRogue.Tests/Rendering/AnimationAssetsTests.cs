using System;
using System.IO;
using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.Rendering;

namespace BlazorRogue.Tests.Rendering;

/// <summary>
/// Checks the generated animation CSS (<see cref="AnimationCssGenerator"/>) against a real atlas.
/// The atlas is licensed-content-derived and never checked into the repo (see tools/AtlasPacker),
/// so this only runs when a developer has BLAZORROGUE_ART_PATH pointed at one locally - absent in
/// CI, same as the loose tileset files used to be. That's the only place a misspelled
/// animationClass (right "animated_" prefix, wrong sprite name) can be caught: it produces
/// internally-consistent CSS that still silently omits keyframe steps at runtime, something
/// <see cref="ConfigurationTests.ParseLoadsHeroesAndMonstersWithAnAnimatedPrefixedAnimationClass"/>
/// can't catch.
/// </summary>
public class AnimationAssetsTests
{
    [Fact]
    public void GeneratedAnimationsReferenceSpritesThatActuallyExistInTheRealAtlas()
    {
        string? artPath = Environment.GetEnvironmentVariable("BLAZORROGUE_ART_PATH");
        if (artPath is null || !File.Exists(Path.Combine(artPath, "atlas.json")))
        {
            return;
        }

        var configuration = new Configuration();
        configuration.Parse();
        var atlas = SpriteAtlas.Load(artPath);

        var spriteNames = configuration
            .HeroTypes.Values.Concat(configuration.MonsterTypes.Values)
            .Select(t => t.AnimationClass)
            .Where(c => c.StartsWith("animated_", StringComparison.Ordinal))
            .Select(c => c["animated_".Length..])
            .Distinct();

        Assert.NotEmpty(spriteNames);
        Assert.All(
            spriteNames,
            spriteName =>
                Assert.All(
                    Enumerable.Range(0, 4),
                    frame =>
                        Assert.True(
                            atlas.CssDeclarationsFor(spriteName, frame) is not null,
                            $"Missing atlas sprite/frame: {spriteName} frame {frame}"
                        )
                )
        );
    }
}
