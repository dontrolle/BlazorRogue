using System;
using System.Collections.Generic;

namespace BlazorRogue
{
    public class Game
    {
        const int Width = 30;
        const int Height = 22;

        public DungeonGenerator DungeonGenerator { get; private set; } 
        public Map Map { get; private set; }
        public static SoundManager SoundManager { get; set; }

        public Game()
        {
            DungeonGenerator = new DungeonGenerator(Width, Height);
            Map = DungeonGenerator.GenerateMap();
        }
    }
}
