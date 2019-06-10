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
        // TODO: Maybe all bottom (defined how?) walls should just have half wall tiles as decoration? Would match examples...
        if (Orientation == Orientation.Vertical)
        {
            if(IsOpen)
            {
                // place 3 just above door tile
                map.Decorations[x, y - 1].Add( new Decoration( this, ImagePrefix + 3 ));
                // place 7 on door tile
                map.Decorations[x, y].Add( new Decoration ( this, ImagePrefix + 7 ));
                // place 10 on door tile
                map.Decorations[x, y].Add( new Decoration ( this, ImagePrefix + 10 ) );
                // place half a wall tile below
                map.Decorations[x, y].Add( new Decoration ( this, "wall_" + map.DungeonWallSet + "_" + HalfWallIndex ) { Interact = Interact });
            }
            else
            {
                // place 2 just above door tile
                map.Decorations[x, y - 1].Add( new Decoration ( this, ImagePrefix + 2 ));
                // place 6 on door tile
                map.Decorations[x, y].Add( new Decoration ( this, ImagePrefix + 6 ) );
                // place half a wall tile below
                map.Decorations[x, y].Add( new Decoration ( this, "wall_" + map.DungeonWallSet + "_" + HalfWallIndex ){ Interact = Interact });
            }
        }
        else
        {
            // TODO:
        }
    }

    public void Interact()
    {
        IsOpen = !IsOpen;

        // TODO: Optimization - On Interact(), GameObject has ability to remove all it's Decorations, and just call Render on itself.
    }
}