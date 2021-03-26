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
        public static SoundManager SoundManager { get; set; }
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
