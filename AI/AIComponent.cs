using BlazorRogue.Combat.Warhammer;
using BlazorRogue.Components;
using BlazorRogue.World;

namespace BlazorRogue.AI;

abstract class AIComponent(Map map) : Component()
{
    protected readonly Map map = map;
    public bool Awake { get; protected set; }

    /// <summary>Returns the monster's own attack outcome, or null if it didn't attack this turn.</summary>
    public abstract AttackResult? TakeTurn();

    public void Wake()
    {
        if (Awake)
            return;

        Awake = true;
        References.Game.AddMessage($"The {Owner!.Name} awake{(Owner!.Singular ? "s" : "")}.");
    }
}
