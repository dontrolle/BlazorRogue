using System.Collections.Generic;
using System.IO;
using BlazorRogue.Rendering;

namespace BlazorRogue.Tests.Rendering;

public class SpriteAtlasTests
{
    [Fact]
    public void LoadWithNullArtPathIsUnavailable()
    {
        var atlas = SpriteAtlas.Load(null);

        Assert.False(atlas.IsAvailable);
        Assert.Equal("", atlas.GenerateStaticCss());
        Assert.Null(atlas.CssDeclarationsFor("anything"));
        Assert.Empty(atlas.SheetEndpoints);
    }

    [Fact]
    public void LoadWithAMissingAtlasJsonIsUnavailable()
    {
        string emptyDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var atlas = SpriteAtlas.Load(emptyDir);

            Assert.False(atlas.IsAvailable);
        }
        finally
        {
            Directory.Delete(emptyDir, recursive: true);
        }
    }

    [Fact]
    public void LoadReadsARealAtlasJsonFileFromDisk()
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(dir, "atlas.json"),
                """
                {
                  "Sheets": { "uf_terrain": { "Width": 100, "Height": 200, "File": "0.bin" } },
                  "Sprites": { "wall_dungeon_1": { "Sheet": "uf_terrain", "X": 25, "Y": 50, "W": 50, "H": 100 } }
                }
                """
            );

            var atlas = SpriteAtlas.Load(dir);

            Assert.True(atlas.IsAvailable);
            Assert.Equal(["/a/0.bin"], atlas.SheetEndpoints);
            Assert.True(atlas.IsKnownSheetFile("0.bin"));
            Assert.False(atlas.IsKnownSheetFile("1.bin"));
            Assert.Equal(
                "background-image:var(--atlas-0);background-size:200% 200%;background-position:50% 50%;",
                atlas.CssDeclarationsFor("wall_dungeon_1")
            );
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    static AtlasJsonDocument SingleSpriteDocument(
        int sheetWidth,
        int sheetHeight,
        int x,
        int y,
        int w,
        int h,
        int frameCount = 1,
        int frameStrideX = 0,
        int frameStrideY = 0,
        string sheetName = "uf_terrain"
    ) =>
        new(
            Sheets: new Dictionary<string, SpriteSheetInfo>
            {
                [sheetName] = new(sheetWidth, sheetHeight, "0.bin"),
            },
            Sprites: new Dictionary<string, SpriteAtlasEntry>
            {
                ["sprite"] = new(sheetName, x, y, w, h, frameCount, frameStrideX, frameStrideY),
            }
        );

    [Fact]
    public void CssDeclarationsForComputesPercentageSizeAndPosition()
    {
        // Round numbers throughout so the expected string is exact, not just "close": a 100x200
        // sheet, sprite at (25,50) sized 50x100 -> sizes double (2x/2x) and the sprite sits at the
        // midpoint of the position range on both axes.
        var atlas = SpriteAtlas.FromDocument(SingleSpriteDocument(100, 200, 25, 50, 50, 100));

        Assert.Equal(
            "background-image:var(--atlas-0);background-size:200% 200%;background-position:50% 50%;",
            atlas.CssDeclarationsFor("sprite")
        );
    }

    [Fact]
    public void CssDeclarationsForGuardsAgainstDivideByZeroWhenASpriteFillsTheWholeSheetWidth()
    {
        // Sprite as wide as its sheet - the position denominator (sheetWidth - spriteWidth) would
        // be zero; must fall back to 0% rather than throwing or emitting NaN/Infinity.
        var atlas = SpriteAtlas.FromDocument(SingleSpriteDocument(100, 100, 0, 25, 100, 50));

        Assert.Equal(
            "background-image:var(--atlas-0);background-size:100% 200%;background-position:0% 50%;",
            atlas.CssDeclarationsFor("sprite")
        );
    }

    [Fact]
    public void CssDeclarationsForReturnsNullForAnUnknownSpriteKey()
    {
        var atlas = SpriteAtlas.FromDocument(SingleSpriteDocument(100, 100, 0, 0, 50, 50));

        Assert.Null(atlas.CssDeclarationsFor("does_not_exist"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void CssDeclarationsForReturnsNullForAFrameOutsideTheStaticSpritesSingleFrame(int frame)
    {
        var atlas = SpriteAtlas.FromDocument(SingleSpriteDocument(100, 100, 0, 0, 50, 50));

        Assert.Null(atlas.CssDeclarationsFor("sprite", frame));
    }

    [Fact]
    public void CssDeclarationsForOffsetsEachFrameByTheStride()
    {
        var atlas = SpriteAtlas.FromDocument(
            SingleSpriteDocument(
                200,
                100,
                x: 0,
                y: 0,
                w: 50,
                h: 100,
                frameCount: 4,
                frameStrideX: 50
            )
        );

        // background-size never changes across frames (same W/H every frame); only the position
        // shifts by frameStrideX per step.
        Assert.Equal(
            "background-image:var(--atlas-0);background-size:400% 100%;background-position:0% 0%;",
            atlas.CssDeclarationsFor("sprite", 0)
        );
        Assert.Equal(
            "background-image:var(--atlas-0);background-size:400% 100%;background-position:33.3333% 0%;",
            atlas.CssDeclarationsFor("sprite", 1)
        );
        Assert.Equal(
            "background-image:var(--atlas-0);background-size:400% 100%;background-position:100% 0%;",
            atlas.CssDeclarationsFor("sprite", 3)
        );
    }

    [Fact]
    public void SheetIndexesAreAssignedInAlphabeticalOrderRegardlessOfInsertionOrder()
    {
        var document = new AtlasJsonDocument(
            Sheets: new Dictionary<string, SpriteSheetInfo>
            {
                ["zzz_sheet"] = new(100, 100, "z.bin"),
                ["aaa_sheet"] = new(100, 100, "a.bin"),
            },
            Sprites: new Dictionary<string, SpriteAtlasEntry>
            {
                ["from_z"] = new("zzz_sheet", 0, 0, 50, 50),
                ["from_a"] = new("aaa_sheet", 0, 0, 50, 50),
            }
        );
        var atlas = SpriteAtlas.FromDocument(document);

        Assert.Equal(["/a/a.bin", "/a/z.bin"], atlas.SheetEndpoints);
        Assert.Contains("var(--atlas-1)", atlas.CssDeclarationsFor("from_z"));
        Assert.Contains("var(--atlas-0)", atlas.CssDeclarationsFor("from_a"));
    }

    [Fact]
    public void GenerateStaticCssEmitsOneRulePerSprite()
    {
        var document = new AtlasJsonDocument(
            Sheets: new Dictionary<string, SpriteSheetInfo> { ["sheet"] = new(100, 100, "0.bin") },
            Sprites: new Dictionary<string, SpriteAtlasEntry>
            {
                ["a"] = new("sheet", 0, 0, 50, 50),
                ["b"] = new("sheet", 50, 50, 50, 50),
            }
        );
        var atlas = SpriteAtlas.FromDocument(document);

        var css = atlas.GenerateStaticCss();

        Assert.Contains($".spr-a {{ {atlas.CssDeclarationsFor("a")} }}", css);
        Assert.Contains($".spr-b {{ {atlas.CssDeclarationsFor("b")} }}", css);
    }
}
