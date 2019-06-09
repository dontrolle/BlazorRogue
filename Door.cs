using System;

public class Door : GameObject {
    // metal, stone, wood, ruin... 
    // TODO collect together with other lists of strings
    public string DoorType;

    public Orientation Orientation { get; set; }

    public bool IsOpen { get; set; }

    private string ImagePrefix { get => "door_" + DoorType + "_"; }

    public override void RenderAt (Decoration[,] decorations, string wallSet)
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
                //map[x, y - 1]. = 
                // place 6 on door tile
                // place half a wall tile below
            }
        }
        else
        {
            // TODO
        }
    }
}