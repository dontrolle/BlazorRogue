using System;

using BlazorRogue.Combat.Warhammer;

namespace BlazorRogue
{
  public class Game
  {
    const int Width = 38;
    const int Height = 30;

    public DungeonGenerator DungeonGenerator { get; private set; }
    public Map Map { get; private set; }
    // Set up before the first Game instance is constructed; null! avoids forcing nullable-checks
    // throughout the codebase for a value that's always non-null in practice.
    public static SoundManager SoundManager { get; set; } = null!;
    public FightingSystem FightingSystem { get; private set; }
    public Configuration Configuration { get; private set; }
    public EffectsSystem EffectsSystem { get; private set; }

    public Game()
    {
      Configuration = new Configuration();
      References.Configuration = Configuration;

      // TODO pass as async 
      Configuration.Parse();
      DungeonGenerator = new DungeonGenerator(Width, Height, this);
      FightingSystem = new FightingSystem(this);

      Map = DungeonGenerator.GenerateMap();
      References.Map = Map;

      EffectsSystem = new EffectsSystem();
      References.EffectsSystem = EffectsSystem;
    }
  }
}
