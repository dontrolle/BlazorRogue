using System;
using BlazorRogue.AI;
using BlazorRogue.Combat.Warhammer;
using BlazorRogue.Components;
using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

/// <summary>
/// GameObjects are all sorts of objects.
/// They know how to be rendered in to one of more Decorations on this or the surrounding tiles.
/// </summary>
abstract class GameObject
{
    public int X { get; protected set; }
    public int Y { get; protected set; }
    public bool Blocking { get; set; }
    public bool BlocksLight { get; set; }

    public bool InvisibleOutsideFov { get; set; }
    public string Name { get; private set; }

    /// <summary>
    /// Is the object a singular entity (like a single goblin?) or is it multiple (like a swarm of flies)?
    /// </summary>
    public bool Singular { get; internal set; } = true;
    public virtual string InfoText { get; set; } = "";

    // Components
    public AIComponent? AIComponent { get; protected set; }
    public CombatComponent? CombatComponent { get; }
    public UseableComponent? UseableComponent { get; }
    public InventoryComponent? InventoryComponent { get; }

    public event EventHandler? GameObjectKilled;

    protected GameObject(
        int x,
        int y,
        string name,
        AIComponent? aIComponent = null,
        CombatComponent? combatComponent = null,
        UseableComponent? useableComponent = null,
        InventoryComponent? inventoryComponent = null
    )
    {
        X = x;
        Y = y;
        Name = name;

        AIComponent = aIComponent;
        AIComponent?.SetOwner(this);

        CombatComponent = combatComponent;
        CombatComponent?.SetOwner(this);

        UseableComponent = useableComponent;
        UseableComponent?.SetOwner(this);

        InventoryComponent = inventoryComponent;
        InventoryComponent?.SetOwner(this);
    }

    public abstract void Render(Map map);

    public virtual void Move(int xDelta, int yDelta)
    {
        X += xDelta;
        Y += yDelta;
    }

    /// <summary>
    /// Teleports the object to an absolute position, e.g. when placing an existing player on a
    /// freshly generated map. Unlike <see cref="Move"/>, this isn't a movement action - it has no
    /// side effects (no walk sound, etc).
    /// </summary>
    internal void PlaceAt(int x, int y)
    {
        X = x;
        Y = y;
    }

    protected virtual void OnGameObjectKilled(EventArgs e) => GameObjectKilled?.Invoke(this, e);

    internal void Kill()
    {
        References.SoundManager.PlayKillMonsterSound();
        References.Game.AddMessage($"{Name} was killed!");
        OnGameObjectKilled(new EventArgs());
    }
}
