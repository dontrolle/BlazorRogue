using BlazorRogue.Components;
using BlazorRogue.World;

namespace BlazorRogue.AI;

abstract class AIComponent(Map map) : Component()
{
    protected readonly Map map = map;
    public bool Awake { get; protected set; }

    public abstract void TakeTurn();

    public void Wake()
    {
        if (Awake)
            return;

        Awake = true;
        References.Game.AddMessage($"{Owner!.Name} awake{(Owner!.Singular ? "s" : "")}");
    }
}
