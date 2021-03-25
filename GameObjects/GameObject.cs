using System;
using BlazorRogue.AI;
using BlazorRogue.Combat.Warhammer;

namespace BlazorRogue.GameObjects
{
    /// <summary>
    /// GameObjects are all sorts of objects.
    /// They know how to be rendered in to one of more Decorations on this or the surrounding tiles. 
    /// </summary>
    public abstract class GameObject
    {
        public int x { get; protected set; }
        public int y { get; protected set; }
        public bool Blocking { get; set; } = false;
        public bool BlocksLight { get; set; } = false;

        public bool InvisibleOutsideFov { get; set; } = false;
        public string Name { get; private set; }
        public virtual string? InfoText => "";

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
            InventoryComponent? inventoryComponent = null)
        {
            this.x = x;
            this.y = y;
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
            x += xDelta;
            y += yDelta;
        }

        protected virtual void OnGameObjectKilled(EventArgs e)
        {
            GameObjectKilled?.Invoke(this, e);
        }

        internal void Kill()
        {
            References.SoundManager.PlayKillMonsterSound();
            OnGameObjectKilled(new EventArgs());
        }
    }
}
