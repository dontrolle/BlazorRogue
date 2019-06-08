using System;

public class DungeonGenerator {

    String[] prefixes = new String[] { "ground" };
    String[] middles = new String[] { 
        "crusted", 
        "dirt_brown", 
        "dirt_dark",
        "grass", 
        "grass_burnt", 
        "sand" 
        };

    private MapPosition[,] map;

    public MapPosition[,] Map
    {
        get
        {
            return map;
        }
    }

    public DungeonGenerator(int width, int height)
    {
        map = new MapPosition[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                map[i,j] = new MapPosition {
                    x = i,
                    y = j,
                    ImageName = "floor_extra_11"
                };
            }
        }
    }

    public void Generate(){
        
    }
}