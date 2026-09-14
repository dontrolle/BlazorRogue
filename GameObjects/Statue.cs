using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

/// <summary>
/// A two-tile-tall statue. Its own tile isn't Blocking - the art fits a moveable standing in front
/// of it - but it acts as a fence (see <see cref="Edge"/>) across the edge shared with the tile
/// above, where its "top" half is drawn (like Door's raised crown or Torch's wall flame): you can
/// stand right behind it, but can't step directly between the two tiles.
/// </summary>
class Statue : GameObject
{
    static StaticDecorativeObjectType Sdot =>
        References.Configuration.StaticDecorativeObjectTypes["statue"];

    public override string InfoText => Sdot.InfoText;

    public Statue(int x, int y)
        : base(x, y, Sdot.Name)
    {
        Blocking = Sdot.Blocking;
        BlockedEdges = Sdot.BlockedEdges;
        OccupiesTile = Sdot.OccupiesTile;
    }

    public override void Render(Map map)
    {
        var sdot = Sdot;

        map.Decorations[X, Y]
            .Add(new Decoration(this, sdot.ImageVariants["top"]) { VerticalOffset = -1 });
        map.Decorations[X, Y]
            .Add(
                new Decoration(this, sdot.ImageVariants["bottom"])
                {
                    Character = sdot.Character,
                    CharacterColor = sdot.CharacterColor,
                    MakeCoveringOffsetDecsTransparent = sdot.MakeCoveringOffsetDecsTransparent,
                }
            );
    }
}
