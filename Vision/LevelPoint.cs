namespace BlazorRogue.Vision;

struct LevelPoint(uint x, uint y) : System.IEquatable<LevelPoint>
{
    public uint X = x;
    public uint Y = y;

    public override readonly bool Equals(object? obj) => obj is LevelPoint other && Equals(other);

    public override readonly int GetHashCode() => throw new System.NotImplementedException();

    public static bool operator ==(LevelPoint left, LevelPoint right) => left.Equals(right);

    public static bool operator !=(LevelPoint left, LevelPoint right) => !(left == right);

    public readonly bool Equals(LevelPoint other) => X == other.X && Y == other.Y;
}
