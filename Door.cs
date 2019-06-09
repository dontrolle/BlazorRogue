using System;

public class Door : GameObject {
    public string DoorType { get; set; }

    public int HalfWallIndex { get; set; }

    public Orientation Orientation { get; set; }

    public bool IsOpen { get; set; }

    private string ImagePrefix { get => "door_" + DoorType + "_"; }

    public Door(int x, int y, string doorType, int halfWallIndex, Orientation orientation, bool isOpen) : base(x,y)
    {
        DoorType = doorType;
        HalfWallIndex = halfWallIndex;
        Orientation = orientation;
        IsOpen = isOpen;
    }

    public override void Render(Map map)
    {
        if (Orientation == Orientation.Vertical)
        {
            if(IsOpen)
            {
                //TODO
            }
            else
            {
                // place 2 just above door tile
                map.Decorations[x, y - 1].Add( new Decoration { ImageName = ImagePrefix + 2 });
                // place 6 on door tile
                map.Decorations[x, y].Add( new Decoration { ImageName = ImagePrefix + 6 });
                // place half a wall tile below
                map.Decorations[x, y].Add( new Decoration { ImageName = "wall_" + map.DungeonWallSet + "_" + HalfWallIndex });
            }
        }
        else
        {
            // TODO
        }
    }
}