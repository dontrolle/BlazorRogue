using BlazorRogue.Entities;

namespace BlazorRogue.Tests
{
  public class MapTests
  {
    private static Map CreateMap(int width = 10, int height = 10)
    {
      var wallSet = new TileSet("test_wall", TileType.Wall, "test", new[] { 0 });
      return new Map(width, height, wallSet, game: null!);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 4, 5)]
    [InlineData(6, 8, 10)]
    public void GetDistance_ComputesEuclideanDistanceTruncated(int dx, int dy, int expected)
    {
      Assert.Equal(expected, Map.GetDistance(dx, dy));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 4, 25)]
    [InlineData(-3, 4, 25)]
    public void GetDistanceSquared_ComputesSquaredDistance(int dx, int dy, int expected)
    {
      Assert.Equal(expected, Map.GetDistanceSquared(dx, dy));
    }

    [Fact]
    public void ForEachTile_VisitsEveryTileExactlyOnce()
    {
      var map = CreateMap(width: 4, height: 3);
      var visited = new List<(int x, int y)>();

      map.ForEachTile((x, y) => visited.Add((x, y)));

      Assert.Equal(12, visited.Count);
      Assert.Equal(12, visited.Distinct().Count());
      Assert.Contains((0, 0), visited);
      Assert.Contains((3, 2), visited);
    }

    [Fact]
    public void ForEachTile_ClipsBoundsToMapDimensions()
    {
      var map = CreateMap(width: 5, height: 5);
      var visited = new List<(int x, int y)>();

      // Requesting a much larger area than the map should silently clip, not throw.
      map.ForEachTile((x, y) => visited.Add((x, y)), xMin: -10, xMax: 100, yMin: -10, yMax: 100);

      Assert.Equal(25, visited.Count);
    }

    [Fact]
    public void BlocksLight_ReturnsTrueOutsideMapBounds()
    {
      var map = CreateMap(width: 5, height: 5);

      Assert.True(map.BlocksLight(-1, 0));
      Assert.True(map.BlocksLight(0, -1));
      Assert.True(map.BlocksLight(5, 0));
      Assert.True(map.BlocksLight(0, 5));
    }

    [Fact]
    public void SetVisible_MarksTileAsVisibleAndMapped()
    {
      var map = CreateMap();

      map.SetVisible(2, 3);

      Assert.True(map.IsVisibleMap[2, 3]);
      Assert.True(map.IsMappedMap[2, 3]);
    }

    [Fact]
    public void SetVisible_OutOfBoundsIsANoOp()
    {
      var map = CreateMap(width: 5, height: 5);

      // Should not throw despite being out of bounds.
      map.SetVisible(-1, 0);
      map.SetVisible(0, 10);
    }

    [Fact]
    public void IsBlocked_BeforePostGenInitialize_ReflectsTileBlockingState()
    {
      var map = CreateMap();

      // Tiles start out as blocking "dark" placeholder tiles until dungeon generation carves them out.
      Assert.True(map.IsBlocked(2, 2));

      map.Tiles[2, 2].Blocking = false;

      Assert.False(map.IsBlocked(2, 2));
    }
  }
}
