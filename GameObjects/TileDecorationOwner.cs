using System;
using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

// Exists solely so Tile.Render can hand Decoration a valid owner - Decoration.GameObject is
// non-nullable (GamePage.razor reads .X/.Y/.Name/.Blocking/.InvisibleOutsideFov/.InfoText off it
// unconditionally, e.g. on click). Tile builds and owns the actual decoration logic; this never
// renders itself and is never added to Map.GameObjects. The name defaults to "Wall" (the original
// and still most common use - wall dressing); liquid pools pass their own so a hovered pool tile
// reads sensibly in the dev alt-text.
class TileDecorationOwner(int x, int y, string name = "Wall") : GameObject(x, y, name)
{
    public override void Render(Map map) =>
        throw new InvalidOperationException(
            $"{nameof(TileDecorationOwner)} is only ever used as a {nameof(Decoration)} owner - Tile builds its decorations directly and never calls Render() on this."
        );
}
