using System;
using BlazorRogue.Components;
using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

enum StairDirection
{
    Up,
    Down,
}

class Stair(int x, int y, StairDirection direction)
    : GameObject(x, y, NameFor(direction), useableComponent: new UseableComponent(Use))
{
    public StairDirection Direction { get; } = direction;

    // Weighted-picked once (see Render) and then reused on every subsequent Render call, so a
    // stair with multiple weighted image options doesn't change its displayed art between
    // re-renders.
    string? pickedImageName;

    static string NameFor(StairDirection direction) =>
        direction switch
        {
            StairDirection.Down => "Stairs down",
            StairDirection.Up => "Stairs up",
            _ => throw new InvalidOperationException($"Unknown StairDirection: {direction}"),
        };

    static string CharacterFor(StairDirection direction) =>
        direction switch
        {
            StairDirection.Down => ">",
            StairDirection.Up => "<",
            _ => throw new InvalidOperationException($"Unknown StairDirection: {direction}"),
        };

    const string CharacterColor = "White";

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
        var tileSet = map.Tiles[X, Y].TileSet;
        var stairsSource = tileSet.StairImages is not null
            ? tileSet
            : References.Configuration.DefaultStairsFloorSet;
        var (up, down) = stairsSource.StairImages!.Value;
        pickedImageName ??= TileSet.PickWeighted(
            Direction == StairDirection.Down ? down : up,
            Random.Shared
        );

        map.Decorations[X, Y]
            .Add(
                new Decoration(this, pickedImageName)
                {
                    Character = CharacterFor(Direction),
                    CharacterColor = CharacterColor,
                    OnUse = UseableComponent!.Use,
                    MakeCoveringOffsetDecsTransparent = true,
                }
            );
    }
}
