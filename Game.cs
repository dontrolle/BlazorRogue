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
        
        public Game()
        {
            var config = new Configuration();
            // TODO pass as async 
            config.Parse();
            DungeonGenerator = new DungeonGenerator(Width, Height, this, config);
            FightingSystem = new FightingSystem(this);
            Map = DungeonGenerator.GenerateMap();
        }
    }
}
