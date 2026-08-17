using System;
using BlazorRogue.Components;
using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

enum StairDirection
{
    Up,
    Down,
}

class Stair(int x, int y, StairDirection direction)
    : GameObject(
        x,
        y,
        References.Configuration.StaticDecorativeObjectTypes[IdFor(direction)].Name,
        useableComponent: new UseableComponent(Use)
    )
{
    public StairDirection Direction { get; } = direction;

    static string IdFor(StairDirection direction) =>
        direction switch
        {
            StairDirection.Down => "stairs_down",
            StairDirection.Up => "stairs_up",
            _ => throw new InvalidOperationException($"Unknown StairDirection: {direction}"),
        };

    internal static void Use(GameObject go)
    {
        if (go is Stair stair)
        {
            References.Game.TransitionToLevel(stair.Direction);
        }
        else
        {
            throw new InvalidOperationException(
                $"{nameof(Use)} called with {nameof(GameObject)} not of type {nameof(Stair)}."
            );
        }
    }

    public override void Render(Map map)
    {
        var sdot = References.Configuration.StaticDecorativeObjectTypes[IdFor(Direction)];

        map.Decorations[X, Y]
            .Add(
                new Decoration(this, sdot.ImageVariants[""], sdot.ImgFolder)
                {
                    Character = sdot.Character,
                    CharacterColor = sdot.CharacterColor,
                    OnUse = UseableComponent!.Use,
                    MakeCoveringOffsetDecsTransparent = sdot.MakeCoveringOffsetDecsTransparent,
                }
            );
    }
}
