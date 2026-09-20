using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

/// <summary>
/// A fountain, anchored at the floor tile holding its basin. Like Statue, the art comfortably fits
/// a moveable standing on/in front of it, so this tile isn't Blocking - but still needs
/// OccupiesTile so no other solid prop lands on it. Two animated overlays are layered on top of the
/// static basin: a splash on the fountain's own tile, and a subtler droplet that bleeds visually
/// onto the wall tile directly above (VerticalOffset -1, same technique as Statue's "top" half) -
/// no Edge/BlockedEdges fencing is needed for that bleed, unlike Statue's, since the tile it lands
/// on is a Wall tile, already unconditionally impassable regardless of any GameObject there.
/// </summary>
class Fountain : GameObject
{
    static StaticDecorativeObjectType Sdot =>
        References.Game.Configuration.StaticDecorativeObjectTypes["fountain"];

    public override string InfoText => Sdot.InfoText;

    public Fountain(int x, int y)
        : base(x, y, Sdot.Name)
    {
        Blocking = Sdot.Blocking;
        OccupiesTile = Sdot.OccupiesTile;
    }

    public override void Render(Map map)
    {
        var sdot = Sdot;

        map.Decorations[X, Y]
            .Add(
                new Decoration(this, sdot.ImageVariants["pool"])
                {
                    DecorationLayer = Decoration.Layer.Behind,
                    Character = sdot.Character,
                    CharacterColor = sdot.CharacterColor,
                }
            );

        map.Decorations[X, Y]
            .Add(
                new Decoration(this, null) { AnimationClass = sdot.AnimationClassForTag("floor") }
            );

        map.Decorations[X, Y]
            .Add(
                new Decoration(this, null)
                {
                    AnimationClass = sdot.AnimationClassForTag("wall"),
                    VerticalOffset = -1,
                }
            );
    }
}
