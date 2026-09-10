namespace AtlasPacker.Tests;

public class HeroBlockTests
{
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(144, 0, 3, 0)]
    [InlineData(384, 192, 8, 4)]
    // Autocropped content need not start flush at its cell's top-left corner: a match a few pixels
    // inside a cell still belongs to that cell, not a "misaligned" non-cell.
    [InlineData(386, 576, 8, 12)]
    public void IdentityFromSimpleSheetPositionFloorDividesToTheContainingCell(
        int x,
        int y,
        int expectedCol,
        int expectedRow
    )
    {
        Assert.Equal((expectedCol, expectedRow), HeroBlock.IdentityFromSimpleSheetPosition(x, y));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void IdentityFromSimpleSheetPositionThrowsForANegativePosition(int x, int y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HeroBlock.IdentityFromSimpleSheetPosition(x, y)
        );
    }

    [Theory]
    [InlineData(3, 0, 0, 576, 0)]
    [InlineData(3, 0, 1, 624, 0)]
    [InlineData(3, 0, 3, 720, 0)]
    [InlineData(8, 4, 0, 1536, 192)]
    public void FrameOriginComputesTheBlockOffsetInTheFullSheet(
        int col,
        int row,
        int frame,
        int expectedX,
        int expectedY
    )
    {
        Assert.Equal((expectedX, expectedY), HeroBlock.FrameOrigin(col, row, frame));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void FrameOriginThrowsForAnOutOfRangeFrame(int frame)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HeroBlock.FrameOrigin(0, 0, frame));
    }
}
