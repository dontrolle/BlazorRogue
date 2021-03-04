using System;
using System.Collections.Generic;

namespace BlazorRogue.GameObjects
{
    public class Door : GameObject
    {
        public string DoorType { get; private set; }
        public int HalfWallIndex { get; private set; }
        public Orientation Orientation { get; private set; }
        public bool IsOpen { get; private set; }
        private string ImagePrefix { get => "door_" + DoorType + "_"; }

        public Door(int x, int y, string doorType, int halfWallIndex, Orientation orientation, bool isOpen) : base(x, y, "Door")
        {
            DoorType = doorType;
            HalfWallIndex = halfWallIndex;
            Orientation = orientation;
            IsOpen = isOpen;
            Blocking = BlocksLight = !isOpen;
        }

        public override void Render(Map map)
        {
            // TODO: Maybe all bottom (defined how?) walls should just have half wall tiles as decoration? Would match examples...
            if (Orientation == Orientation.Vertical)
            {
                if (IsOpen)
                {
                    // place 3 just above door tile
                    map.Decorations[x, y].Add(new Decoration(this, ImagePrefix + 3) { Offset = -Map.DEPRECATE_TileHeight });
                    // place 7 on door tile
                    map.Decorations[x, y].Add(new Decoration(this, ImagePrefix + 7));
                    // place 10 on door tile - raised 1 z-index, to be in front of player
                    map.Decorations[x, y].Add(new Decoration(this, ImagePrefix + 10) { InFront = true });
                }
                else
                {
                    // place 2 just above door tile
                    map.Decorations[x, y].Add(new Decoration(this, ImagePrefix + 2) { Offset = -Map.DEPRECATE_TileHeight });
                    // place 6 on door tile
                    map.Decorations[x, y].Add(new Decoration(this, ImagePrefix + 6));
                }
            }
            else if (Orientation == Orientation.Horizontal)
            {
                if (IsOpen)
                {
                    // place 1 above door tile
                    map.Decorations[x, y].Add(new Decoration(this, ImagePrefix + 1) { Offset = -Map.DEPRECATE_TileHeight });
                    // place 5 on door tile
                    map.Decorations[x, y].Add(new Decoration(this, ImagePrefix + 5));
                    // place 9 below door tile
                    map.Decorations[x, y].Add(new Decoration(this, ImagePrefix + 9) { Offset = +Map.DEPRECATE_TileHeight });
                }
                else
                {
                    // place 4 on door tile
                    map.Decorations[x, y].Add(new Decoration(this, ImagePrefix + 4));
                    // place 8 below door tile
                    map.Decorations[x, y].Add(new Decoration(this, ImagePrefix + 8) { Offset = +Map.DEPRECATE_TileHeight });
                }
            }

            // add a button (without own graphic) to interact with the door
            map.Decorations[x, y].Add(new Decoration(this, null) { OnClick = Use });
        }

        public override void Use()
        {
            IsOpen = !IsOpen;
            Blocking = !Blocking;
            BlocksLight = !BlocksLight;
            Game.SoundManager.PlayDoorSound(IsOpen);
        }
    }
}
