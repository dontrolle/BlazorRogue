using System.Collections.Generic;
using BlazorRogue.Entities;
using BlazorRogue.Rendering;

namespace BlazorRogue.Tests.Rendering;

public class AnimationCssGeneratorTests
{
    static MoveableType MakeMoveableType(string id, string animationClass) =>
        new(
            id,
            id,
            animationClass,
            asciiCharacter: "@",
            asciiColour: "white",
            weaponSkill: 1,
            weaponDamage: 1,
            toughness: 1,
            armour: 0,
            wounds: 1,
            aiComponentId: "",
            aiComponentSettings: new SettingsMap(new Dictionary<string, object>()),
            singular: true
        );

    // A single 4-frame atlas entry, matching how tools/AtlasPacker actually records a hero/monster
    // (one strided entry, not 4 separate sprite keys - see HeroBlock there).
    static SpriteAtlas MakeAtlasWithSprite(string spriteName) =>
        SpriteAtlas.FromDocument(
            new AtlasJsonDocument(
                Sheets: new Dictionary<string, SpriteSheetInfo>
                {
                    ["uf_heroes"] = new(Width: 1920, Height: 624, File: "0.bin"),
                },
                Sprites: new Dictionary<string, SpriteAtlasEntry>
                {
                    [spriteName] = new(
                        "uf_heroes",
                        X: 0,
                        Y: 0,
                        W: 48,
                        H: 48,
                        FrameCount: 4,
                        FrameStrideX: 48
                    ),
                }
            )
        );

    [Fact]
    public void GenerateEmitsKeyframesForAllFourFramesLoopingBackToFrameOne()
    {
        var atlas = MakeAtlasWithSprite("templar");

        var css = AnimationCssGenerator.Generate(
            [MakeMoveableType("templar", "animated_templar")],
            atlas
        );

        Assert.Contains("@keyframes templar {", css);
        Assert.Contains($"0% {{ {atlas.CssDeclarationsFor("templar", 0)} }}", css);
        Assert.Contains($"25% {{ {atlas.CssDeclarationsFor("templar", 1)} }}", css);
        Assert.Contains($"50% {{ {atlas.CssDeclarationsFor("templar", 2)} }}", css);
        Assert.Contains($"75% {{ {atlas.CssDeclarationsFor("templar", 3)} }}", css);
        Assert.Contains($"100% {{ {atlas.CssDeclarationsFor("templar", 0)} }}", css);
    }

    [Fact]
    public void GenerateEmitsAnimationClassRuleMatchingTheOriginalHandAuthoredPattern()
    {
        var atlas = MakeAtlasWithSprite("goblin_warrior");

        var css = AnimationCssGenerator.Generate(
            [MakeMoveableType("goblinWarrior", "animated_goblin_warrior")],
            atlas
        );

        Assert.Contains(
            ".animated_goblin_warrior {\n"
                + "  animation-name: goblin_warrior;\n"
                + "  animation-duration: 1.5s;\n"
                + "  animation-iteration-count: infinite;\n"
                + "  animation-timing-function: steps(1);\n"
                + "}",
            css
        );
    }

    [Fact]
    public void GenerateDeduplicatesRepeatedAnimationClassesAcrossHeroesAndMonsters()
    {
        var atlas = MakeAtlasWithSprite("templar");

        var css = AnimationCssGenerator.Generate(
            [
                MakeMoveableType("templar", "animated_templar"),
                MakeMoveableType("otherTemplar", "animated_templar"),
            ],
            atlas
        );

        Assert.Equal(1, CountOccurrences(css, "@keyframes templar {"));
    }

    [Fact]
    public void GenerateSkipsAFrameThatIsMissingFromTheAtlasInsteadOfEmittingAnEmptyStep()
    {
        // Sprite exists but only has 1 frame recorded - frames 1-3 (25%/50%/75%) can't resolve.
        var atlas = SpriteAtlas.FromDocument(
            new AtlasJsonDocument(
                Sheets: new Dictionary<string, SpriteSheetInfo>
                {
                    ["uf_heroes"] = new(Width: 48, Height: 48, File: "0.bin"),
                },
                Sprites: new Dictionary<string, SpriteAtlasEntry>
                {
                    ["templar"] = new("uf_heroes", X: 0, Y: 0, W: 48, H: 48),
                }
            )
        );

        var css = AnimationCssGenerator.Generate(
            [MakeMoveableType("templar", "animated_templar")],
            atlas
        );

        Assert.Contains("0% {", css);
        Assert.Contains("100% {", css);
        Assert.DoesNotContain("25% {", css);
        Assert.DoesNotContain("50% {", css);
        Assert.DoesNotContain("75% {", css);
    }

    static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
