using System;

namespace BlazorRogue.World;

/// <summary>
/// The 8 compass directions a move/attack/use can target, plus <see cref="None"/> for "here" -
/// the numpad '5' wait-in-place/use-here case. <see cref="Map"/> works in Direction throughout;
/// <see cref="DirectionExtensions.FromNumKey"/> is the one remaining bridge back from the numKey
/// char GamePage.razor's browser-key pipeline still produces (see e.g.
/// <see cref="Map.PeekLethalLiquidStep"/>).
/// </summary>
enum Direction
{
    None,
    North,
    South,
    East,
    West,
    NorthEast,
    NorthWest,
    SouthEast,
    SouthWest,
}

static class DirectionExtensions
{
    public static (int Dx, int Dy) ToDelta(this Direction direction) =>
        direction switch
        {
            Direction.None => (0, 0),
            Direction.North => (0, -1),
            Direction.South => (0, 1),
            Direction.East => (1, 0),
            Direction.West => (-1, 0),
            Direction.NorthEast => (1, -1),
            Direction.NorthWest => (-1, -1),
            Direction.SouthEast => (1, 1),
            Direction.SouthWest => (-1, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };

    public static Direction FromNumKey(char numKey) =>
        numKey switch
        {
            '5' => Direction.None,
            '8' => Direction.North,
            '2' => Direction.South,
            '6' => Direction.East,
            '4' => Direction.West,
            '9' => Direction.NorthEast,
            '7' => Direction.NorthWest,
            '3' => Direction.SouthEast,
            '1' => Direction.SouthWest,
            _ => throw new ArgumentOutOfRangeException(
                nameof(numKey),
                numKey,
                "Expected a numpad digit '1'-'9'."
            ),
        };
}
