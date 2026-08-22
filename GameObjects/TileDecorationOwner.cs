using System;
using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

// Exists solely so Tile.Render can hand Decoration a valid owner - Decoration.GameObject is
// non-nullable (GamePage.razor reads .X/.Y/.Name/.Blocking/.InvisibleOutsideFov/.InfoText off it
// unconditionally, e.g. on click). Tile builds and owns the actual decoration logic; this never
// renders itself and is never added to Map.GameObjects.
class TileDecorationOwner(int x, int y) : GameObject(x, y, "Wall")
{
    public override void Render(Map map) =>
        throw new InvalidOperationException(
            $"{nameof(TileDecorationOwner)} is only ever used as a {nameof(Decoration)} owner - Tile builds its decorations directly and never calls Render() on this."
        );
}
