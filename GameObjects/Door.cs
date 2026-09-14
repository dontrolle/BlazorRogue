using System;
using System.Linq;
using BlazorRogue.Components;
using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

/// <summary>
/// An openable door - may be see-through.
/// </summary>
/// <remarks>
/// This class is a bit more hard-wired than others for graphical tileset rendering parts.
/// </remarks>
class Door : GameObject
{
    public string DoorType { get; private set; }
    public Orientation Orientation { get; private set; }
    public bool IsOpen { get; private set; }

    readonly DoorSet doorSet;
    string ImagePrefix => doorSet.ImgPrefix + "_";

    public override string InfoText => $"{doorSet.InfoText} ({(IsOpen ? "open" : "closed")})";

    public Door(int x, int y, string doorType, Orientation orientation, bool isOpen)
        : base(x, y, "Door", null, null, new UseableComponent(Use))
    {
        DoorType = doorType;
        doorSet = References.Configuration.DoorSetById(doorType);
        Orientation = orientation;
        IsOpen = isOpen;
        UpdateBlockingState();
    }

    // A door always blocks movement while closed; whether it also blocks light/vision while closed
    // depends on its door-set (see DoorSet.AlwaysSeeThrough, e.g. a wrought-iron gate you can see
    // through but not walk through).
    void UpdateBlockingState()
    {
        Blocking = !IsOpen;
        BlocksLight = !IsOpen && !doorSet.AlwaysSeeThrough;
    }

    public override void Render(Map map)
    {
        if (Orientation == Orientation.Vertical)
        {
            if (IsOpen)
            {
                // place 3 just above door tile
                map.Decorations[X, Y]
                    .Add(new Decoration(this, ImagePrefix + 3) { VerticalOffset = -1 });
                // place 7 on door tile
                map.Decorations[X, Y]
                    .Add(
                        new Decoration(this, ImagePrefix + 7)
                        {
                            Character = "'",
                            CharacterColor = "White",
                        }
                    );
                // place 10 on door tile - raised 1 z-index, to be in front of player
                map.Decorations[X, Y]
                    .Add(
                        new Decoration(this, ImagePrefix + 10)
                        {
                            DecorationLayer = Decoration.Layer.Infront,
                        }
                    );
            }
            else
            {
                // place 2 just above door tile
                map.Decorations[X, Y]
                    .Add(new Decoration(this, ImagePrefix + 2) { VerticalOffset = -1 });
                // place 6 on door tile
                map.Decorations[X, Y]
                    .Add(
                        new Decoration(this, ImagePrefix + 6)
                        {
                            Character = "+",
                            CharacterColor = "White",
                        }
                    );
            }
        }
        else if (Orientation == Orientation.Horizontal)
        {
            if (IsOpen)
            {
                // place 1 above door tile
                map.Decorations[X, Y]
                    .Add(new Decoration(this, ImagePrefix + 1) { VerticalOffset = -1 });
                // place 5 on door tile
                map.Decorations[X, Y]
                    .Add(
                        new Decoration(this, ImagePrefix + 5)
                        {
                            Character = "'",
                            CharacterColor = "White",
                        }
                    );
                // place 9 below door tile
                map.Decorations[X, Y]
                    .Add(new Decoration(this, ImagePrefix + 9) { VerticalOffset = +1 });
            }
            else
            {
                // place 4 on door tile
                map.Decorations[X, Y]
                    .Add(
                        new Decoration(this, ImagePrefix + 4)
                        {
                            Character = "+",
                            CharacterColor = "White",
                        }
                    );
                // place 8 below door tile
                map.Decorations[X, Y]
                    .Add(new Decoration(this, ImagePrefix + 8) { VerticalOffset = +1 });
            }
        }

        // add a button (without own graphic) to interact with the door
        map.Decorations[X, Y].Add(new Decoration(this, null) { OnUse = UseableComponent!.Use });
        // TODO: Not too happy about this; seems wrong to add mouse interaction decentralized like this, and call UseableComponent.Use from here
    }

    internal static void Use(GameObject go)
    {
        // you can only close a door if no moveable is standing on its tile
        if (References.Map.Moveables.Any(m => m.X == go.X && m.Y == go.Y))
        {
            References.SoundManager.PlayBlockedDoorSound();
            return;
        }
        if (go is Door door)
        {
            door.IsOpen = !door.IsOpen;
            door.UpdateBlockingState();
            References.SoundManager.PlayDoorSound(door.IsOpen);
        }
        else
        {
            throw new InvalidOperationException(
                $"{nameof(Use)} called with {nameof(GameObject)} not of type {nameof(Door)}."
            );
        }
    }
}
