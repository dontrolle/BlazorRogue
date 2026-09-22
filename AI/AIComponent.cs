using BlazorRogue.Components;
using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue.AI;

abstract class AIComponent(Map map) : Component()
{
    protected readonly Map map = map;
    public bool Awake { get; protected set; }

    /// <summary>
    /// Resolves one due turn for this moveable - called only when the map's tick scheduler has
    /// determined it's this moveable's turn, so always represents a real action slot (never "not
    /// my turn yet", which the scheduler itself filters out by only invoking due moveables).
    /// </summary>
    public abstract AITurnOutcome TakeTurn();

    public void Wake()
    {
        if (Awake)
            return;

        Awake = true;
        References.Game.AddMessage($"The {Owner!.Name} awake{(Owner!.Singular ? "s" : "")}.");

        if (Owner is Moveable moveable)
        {
            map.EnqueueMonster(moveable);
        }
    }
}
