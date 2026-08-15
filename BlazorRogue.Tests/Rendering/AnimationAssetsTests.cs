using System.IO;
using System.Text.RegularExpressions;
using BlazorRogue.Entities;
using BlazorRogue.Rendering;

namespace BlazorRogue.Tests.Rendering;

static partial class SpriteUrlRegex
{
    [GeneratedRegex(@"url\('\.\./(img/[^']+)'\)")]
    public static partial Regex Instance();
}

/// <summary>
/// Checks the generated animation CSS (<see cref="AnimationCssGenerator"/>) against the actual
/// sprite files on disk. wwwroot/img/uf_* is the proprietary Ultimate Fantasy tileset - gitignored
/// and absent in CI (see CLAUDE.md) - so this only runs when a licensed copy is present locally,
/// which is the only place a misspelled animationClass (right "animated_" prefix, wrong sprite name)
/// can be caught: it produces internally-consistent CSS that still 404s on nonexistent PNGs at
/// runtime, something <see cref="ConfigurationTests.ParseLoadsHeroesAndMonstersWithAnAnimatedPrefixedAnimationClass"/>
/// can't catch.
/// </summary>
public class AnimationAssetsTests
{
    [Fact]
    public void GeneratedAnimationsReferenceSpriteFilesThatActuallyExist()
    {
        var repoRoot = FindRepoRootWithTileset();
        if (repoRoot == null)
        {
            return;
        }

        var configuration = new Configuration();
        configuration.Parse();
        var css = AnimationCssGenerator.Generate(
            configuration.HeroTypes.Values.Concat(configuration.MonsterTypes.Values)
        );

        var referencedImages = SpriteUrlRegex
            .Instance()
            .Matches(css)
            .Select(match => match.Groups[1].Value);

        Assert.NotEmpty(referencedImages);
        Assert.All(
            referencedImages,
            relativePath =>
                Assert.True(
                    File.Exists(Path.Combine(repoRoot, "wwwroot", relativePath)),
                    $"Missing sprite file: wwwroot/{relativePath}"
                )
        );
    }

    /// <summary>
    /// Walks up from the test binary's output directory looking for a wwwroot/img/uf_heroes folder,
    /// returning null when none is found (i.e. no licensed tileset present, as in CI).
    /// </summary>
    static string? FindRepoRootWithTileset()
    {
        for (
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory != null;
            directory = directory.Parent
        )
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "wwwroot", "img", "uf_heroes")))
            {
                return directory.FullName;
            }
        }
        return null;
    }
}
