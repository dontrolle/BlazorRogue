using BlazorRogue.Entities;

namespace BlazorRogue.Tests
{
  public class ConfigurationTests
  {
    private static Configuration ParseConfiguration()
    {
      var configuration = new Configuration();
      configuration.Parse();
      return configuration;
    }

    [Fact]
    public void Parse_LoadsAllKnownMonsterTypes()
    {
      var configuration = ParseConfiguration();

      Assert.True(configuration.MonsterTypes.ContainsKey("skeleton"));
      Assert.True(configuration.MonsterTypes.ContainsKey("goblin"));
      Assert.True(configuration.MonsterTypes.ContainsKey("ogre"));
    }

    [Fact]
    public void Parse_MonsterStatsMatchDataFile()
    {
      var configuration = ParseConfiguration();

      var ogre = configuration.MonsterTypes["ogre"];
      Assert.Equal("Ogre", ogre.Name);
      Assert.Equal(30, ogre.WeaponSkill);
      Assert.Equal(10, ogre.WeaponDamage);
      Assert.Equal(45, ogre.Toughness);
      Assert.Equal(2, ogre.Armour);
      Assert.Equal(17, ogre.Wounds);
    }

    [Fact]
    public void Parse_LoadsHeroTypes()
    {
      var configuration = ParseConfiguration();

      Assert.True(configuration.HeroTypes.ContainsKey("templar"));
      Assert.Equal("Templar", configuration.HeroTypes["templar"].Name);
    }

    [Fact]
    public void Parse_LoadsFloorAndWallSets()
    {
      var configuration = ParseConfiguration();

      Assert.NotEmpty(configuration.StandardFloorSets);
      Assert.NotEmpty(configuration.DungeonWallSets);
    }

    [Fact]
    public void Parse_LoadsStaticDecorativeObjectTypes()
    {
      var configuration = ParseConfiguration();

      Assert.NotEmpty(configuration.StaticDecorativeObjectTypes);
    }

    [Fact]
    public void Parse_LoadedFloorSetIdsAreUnique()
    {
      // Configuration.Parse() reads from the real Data files, so we cover the duplicate-id guard
      // indirectly here by asserting all currently-loaded floor set ids are unique - a duplicate
      // would previously have caused Parse() itself to throw before we ever got here.
      var configuration = ParseConfiguration();
      var ids = configuration.StandardFloorSets.Select(t => t.Id)
          .Concat(configuration.SpecialFloorSets.Select(t => t.Id));

      Assert.Equal(ids.Count(), ids.Distinct().Count());
    }
  }
}
