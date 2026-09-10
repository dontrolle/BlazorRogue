namespace AtlasPacker;

/// <summary>
/// Maps a character's position in the single-pose identity-lookup sheet to its multi-frame
/// animation block in the full, shipped hero/monster sheet. Both share a grid of
/// <see cref="CellSize"/>px cells; the full sheet is <see cref="FrameCount"/>x wider because each
/// character occupies a run of frames rather than one cell.
/// </summary>
static class HeroBlock
{
    public const int CellSize = 48;
    public const int FrameCount = 4;

    /// <summary>
    /// Converts a matched pixel position in the identity-lookup sheet into the character's grid
    /// identity (cells). Floor division, not exact alignment: autocropped content can land a few
    /// pixels inside its cell rather than flush on the boundary.
    /// </summary>
    public static (int Col, int Row) IdentityFromSimpleSheetPosition(int x, int y)
    {
        if (x < 0 || y < 0)
        {
            throw new ArgumentOutOfRangeException(
                x < 0 ? nameof(x) : nameof(y),
                x < 0 ? x : y,
                "Position must be non-negative."
            );
        }

        return (x / CellSize, y / CellSize);
    }

    /// <summary>
    /// The top-left (x, y) in the full sheet of a given character's animation frame.
    /// </summary>
    public static (int X, int Y) FrameOrigin(int col, int row, int frame)
    {
        if (frame is < 0 or >= FrameCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frame),
                frame,
                $"Frame index must be in [0,{FrameCount})."
            );
        }

        return (col * FrameCount * CellSize + frame * CellSize, row * CellSize);
    }
}
