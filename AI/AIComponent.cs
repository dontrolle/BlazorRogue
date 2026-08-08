using BlazorRogue.Components;
using BlazorRogue.World;

namespace BlazorRogue.AI;

abstract class AIComponent(Map map) : Component()
{
    protected readonly Map map = map;
    public bool Awake { get; protected set; }

    public abstract void TakeTurn();

    public void Wake() => Awake = true;
}
