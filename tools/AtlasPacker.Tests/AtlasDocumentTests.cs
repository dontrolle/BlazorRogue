using System.Text.Json;

namespace AtlasPacker.Tests;

public class AtlasDocumentTests
{
    [Fact]
    public void SpriteEntryFrameOriginComputesStridedOffsetsForAnAnimatedSprite()
    {
        var entry = new SpriteEntry("uf_heroes", 576, 0, 48, 48, FrameCount: 4, FrameStrideX: 48);

        Assert.Equal((576, 0), entry.FrameOrigin(0));
        Assert.Equal((624, 0), entry.FrameOrigin(1));
        Assert.Equal((672, 0), entry.FrameOrigin(2));
        Assert.Equal((720, 0), entry.FrameOrigin(3));
    }

    [Fact]
    public void SpriteEntryFrameOriginThrowsOutOfRangeForAStaticSprite()
    {
        var entry = new SpriteEntry("uf_terrain", 0, 0, 48, 48);

        Assert.Equal((0, 0), entry.FrameOrigin(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => entry.FrameOrigin(1));
    }

    [Fact]
    public void AtlasDocumentRoundTripsThroughJson()
    {
        var document = new AtlasDocument(
            Sheets: new Dictionary<string, SheetInfo> { ["uf_terrain"] = new(960, 1824, "0.bin") },
            Sprites: new Dictionary<string, SpriteEntry>
            {
                ["wall_dungeon_1"] = new("uf_terrain", 10, 20, 48, 48),
                ["archer"] = new("uf_heroes", 576, 0, 48, 48, FrameCount: 4, FrameStrideX: 48),
            }
        );

        string json = JsonSerializer.Serialize(document);
        var roundTripped = JsonSerializer.Deserialize<AtlasDocument>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(document.Sheets, roundTripped.Sheets);
        Assert.Equal(document.Sprites, roundTripped.Sprites);
    }
}
