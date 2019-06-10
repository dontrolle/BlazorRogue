using System;
using System.Collections.Generic;

public class Door : GameObject {
    public string DoorType { get; private set; }
    public int HalfWallIndex { get; private set; }
    public Orientation Orientation { get; private set; }
    public bool IsOpen { get; private set; }
    private string ImagePrefix { get => "door_" + DoorType + "_"; }

    public Door(int x, int y, string doorType, int halfWallIndex, Orientation orientation, bool isOpen) : base(x,y)
    {
        DoorType = doorType;
        HalfWallIndex = halfWallIndex;
        Orientation = orientation;
        IsOpen = isOpen;
    }

    // public override Tuple<List<Tuple<string,int>>,Action> RenderDirect()
    // {
    //     var images = new List<Tuple<string,int>>();
    //     Action action = null;
    //     if (Orientation == Orientation.Vertical)
    //     {
    //         if(IsOpen)
    //         {
    //             // place 3 just above door tile
    //             images.Add(Tuple.Create(ImagePrefix + 3, -Map.TileHeight));
    //             // place 7 on door tile
    //             images.Add(Tuple.Create(ImagePrefix + 7, 0));
    //             // place 10 on door tile
    //             images.Add(Tuple.Create(ImagePrefix + 10, 0));
    //             // place half a wall tile below
    //             images.Add(Tuple.Create("wall_" + HalfWallSet + "_" + HalfWallIndex, 0));
    //             action = Interact;
    //         }
    //         else
    //         {
    //             // place 2 just above door tile
    //             images.Add(Tuple.Create(ImagePrefix + 2, -Map.TileHeight));
    //             // place 6 on door tile
    //             images.Add(Tuple.Create(ImagePrefix + 6, 0));
    //             // place half a wall tile below
    //             images.Add(Tuple.Create("wall_" + HalfWallSet + "_" + HalfWallIndex, 0));
    //             action = Interact;
    //         }
    //     }
    //     else
    //     {
    //         // TODO:
    //     }            

    //     return Tuple.Create(images, action);
    // }

    public override void Render(Map map)
    {
        // TODO: Maybe all bottom (defined how?) walls should just have half wall tiles as decoration? Would match examples...
        if (Orientation == Orientation.Vertical)
        {
            if(IsOpen)
            {
                // place 3 just above door tile
                map.Decorations[x, y].Add( new Decoration( this, ImagePrefix + 3 ) {Offset = -Map.TileHeight});
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
                map.Decorations[x, y].Add( new Decoration ( this, ImagePrefix + 2 ) { Offset = -Map.TileHeight });
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