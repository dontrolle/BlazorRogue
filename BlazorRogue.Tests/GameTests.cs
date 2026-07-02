namespace BlazorRogue.Tests
{
  /// <summary>
  /// End-to-end smoke tests that exercise Configuration parsing + procedural dungeon generation
  /// together, since these are the pieces most likely to break silently (e.g. bad JSON data,
  /// generation getting stuck) without a human noticing until the app is run manually.
  /// </summary>
  public class GameTests
  {
    [Fact]
    public void NewGame_GeneratesAPlayableMapWithAPlacedPlayer()
    {
      var game = new Game();

      Assert.NotNull(game.Map);
      Assert.NotNull(game.Map.Player);
      Assert.InRange(game.Map.Player.x, 0, game.Map.Width - 1);
      Assert.InRange(game.Map.Player.y, 0, game.Map.Height - 1);

      // The underlying tile the player starts on must not itself be a blocking (e.g. wall/void)
      // tile, or the game would be unplayable from the start. Note: Map.IsBlocked() on the
      // player's own coordinates is expected to be true here, since a Moveable standing on a
      // tile marks that tile as blocked for movement purposes (see Map.UpdateBlockMovement()).
      Assert.False(game.Map.Tiles[game.Map.Player.x, game.Map.Player.y].Blocking);
    }

    [Fact]
    public void NewGame_CanBeGeneratedRepeatedlyWithoutThrowing()
    {
      // Dungeon generation involves randomness; run it several times to catch intermittent bugs
      // (e.g. generation logic that occasionally produces an invalid or unreachable layout).
      for (int i = 0; i < 5; i++)
      {
        var game = new Game();
        Assert.NotNull(game.Map.Player);
      }
    }
  }
}
