namespace AtlasPacker;

/// <summary>
/// Finds a sprite crop as a byte-identical sub-image somewhere in a larger sheet - i.e. the exact
/// rectangle an individually-cropped file occupies in the full sheet it was sliced from.
/// </summary>
static class SpriteMatcher
{
    /// <summary>
    /// Returns the top-left (x, y) of the first exact match of <paramref name="sprite"/> within
    /// <paramref name="sheet"/>, scanning top-to-bottom, left-to-right; <see langword="null"/> if
    /// no exact match exists. Both images must already be alpha-normalized the same way (see
    /// <see cref="NormalizedImage"/>) or an otherwise-identical crop will spuriously fail to
    /// match.
    /// </summary>
    public static (int X, int Y)? FindExact(NormalizedImage sheet, NormalizedImage sprite)
    {
        if (sprite.Width > sheet.Width || sprite.Height > sheet.Height)
        {
            return null;
        }

        int maxX = sheet.Width - sprite.Width;
        int maxY = sheet.Height - sprite.Height;
        var firstRowPattern = sprite.Row(0);

        for (int y = 0; y <= maxY; y++)
        {
            var sheetRow = sheet.Row(y);

            // Use the sprite's first row as a search pattern to skip straight to candidate x
            // offsets, rather than testing every (x, y) pixel-by-pixel - a plain O(SW*SH*w*h)
            // brute force is minutes slow on a 1800px-tall sheet across hundreds of sprites.
            int searchStart = 0;
            while (true)
            {
                int foundAt = sheetRow[searchStart..].IndexOf(firstRowPattern);
                if (foundAt < 0)
                {
                    break;
                }

                int byteOffset = searchStart + foundAt;
                searchStart = byteOffset + 1;

                // A real match can only start on a pixel boundary (4 bytes/pixel); a hit at a
                // non-aligned byte offset is a coincidental partial overlap, not a candidate.
                if (byteOffset % 4 != 0)
                {
                    continue;
                }

                int x = byteOffset / 4;
                if (x > maxX)
                {
                    break;
                }

                if (MatchesAt(sheet, sprite, x, y))
                {
                    return (x, y);
                }
            }
        }

        return null;
    }

    static bool MatchesAt(NormalizedImage sheet, NormalizedImage sprite, int x, int y)
    {
        int rowBytes = sprite.Width * 4;
        for (int row = 0; row < sprite.Height; row++)
        {
            var sheetSlice = sheet.Row(y + row).Slice(x * 4, rowBytes);
            if (!sheetSlice.SequenceEqual(sprite.Row(row)))
            {
                return false;
            }
        }

        return true;
    }
}
