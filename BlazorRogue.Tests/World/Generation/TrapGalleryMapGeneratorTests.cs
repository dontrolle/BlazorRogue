using BlazorRogue.Entities;
using BlazorRogue.World;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation;

/// <summary>
/// Guards the trap gallery debug level (see Game.ToggleDebugLevelView): every configured trap type
/// must appear once hidden and once revealed at its documented position, and the level must stay
/// safe to walk (no monsters, no lethal trap total).
/// </summary>
public class TrapGalleryMapGeneratorTests
{
    static LevelConfiguration GalleryLevel() =>
        new(
            number: 0,
            id: "trap-gallery-test-level",
            name: "Trap Gallery Test Level",
            height: 20,
            width: 30,
            generatorId: TrapGalleryMapGenerator.Id,
            backgroundSoundtrack: "test.mp3",
            settingsMap: SettingsMap.Empty
        );

    static Map Generate(Game game) =>
        MapGeneratorFactory.Create(GalleryLevel(), game).GenerateMap();

    [Fact]
    public void HoldsAHiddenAndARevealedTrapForEveryTrapType()
    {
        var game = new Game();
        var map = Generate(game);

        foreach (var (type, index) in game.Configuration.TrapTypes.Select((t, i) => (t, i)))
        {
            var (hx, hy) = TrapGalleryMapGenerator.HiddenPosition(index);
            var (rx, ry) = TrapGalleryMapGenerator.RevealedPosition(index);

            var hidden = Assert.IsType<Trap>(map.Tiles[hx, hy].Trap);
            Assert.Same(type, hidden.Type);
            Assert.Equal(TrapState.Hidden, hidden.State);

            var revealed = Assert.IsType<Trap>(map.Tiles[rx, ry].Trap);
            Assert.Same(type, revealed.Type);
            Assert.Equal(TrapState.Revealed, revealed.State);
        }
    }

    [Fact]
    public void HiddenTrapsProduceNoDecorationAndRevealedOnesDo()
    {
        var game = new Game();
        var map = Generate(game);

        for (int i = 0; i < game.Configuration.TrapTypes.Count(); i++)
        {
            var (hx, hy) = TrapGalleryMapGenerator.HiddenPosition(i);
            var (rx, ry) = TrapGalleryMapGenerator.RevealedPosition(i);
            Assert.Empty(map.Decorations[hx, hy]);
            _ = Assert.Single(map.Decorations[rx, ry]);
        }
    }

    [Fact]
    public void HasNoMonstersAndTheHeroSurvivesSteppingOnEveryTrap()
    {
        var game = new Game();
        var map = Generate(game);

        Assert.Empty(map.Monsters);
        int totalDamage = game.Configuration.TrapTypes.Sum(t => 2 * t.EffectMagnitude);
        Assert.True(totalDamage < map.Player.CombatComponent!.Wounds);
    }
}
