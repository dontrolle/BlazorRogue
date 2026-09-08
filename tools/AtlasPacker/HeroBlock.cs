namespace AtlasPacker;

/// <summary>
/// Maps a hero/monster's position in the small, single-pose <c>uf_heroes_simple.png</c> (used
/// only as an offline identity lookup, never shipped) to its 4-frame animation block in the full
/// <c>uf_heroes.png</c> sheet (the shipped source). Both sheets share the same 10-characters/row,
/// 13-row grid; <c>uf_heroes.png</c> is 4x as wide because each character occupies a
/// <see cref="FrameCount"/>-wide run of frames instead of a single cell (reverse-engineered and
/// visually verified against the real sheets - see a hero's draw-bow cycle and a monster's idle
/// wiggle come out correctly at the computed offsets).
/// </summary>
static class HeroBlock
{
    public const int CellSize = 48;
    public const int FrameCount = 4;

    /// <summary>
    /// Converts an exact-match position in <c>uf_heroes_simple.png</c> (pixels) into the
    /// character's grid identity (cells). Uses floor division rather than requiring exact
    /// alignment: many exported crops are autocropped tighter than their full 48px cell (a
    /// character's opaque content need not start flush at the cell's top-left corner), so the
    /// matched position can legitimately land a few pixels inside its cell rather than exactly on
    /// its boundary - a real run against the licensed assets confirmed this (e.g. a match at
    /// x=386 belongs to the cell at col=8, not to some misaligned non-cell).
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
    /// The top-left (x, y) in <c>uf_heroes.png</c> of a given character's animation frame.
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
