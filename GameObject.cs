using System;

namespace BlazorRogue
{
    /// <summary>
    /// GameObjects are all sorts of objects.
    /// They know how to be rendered in to one of more decorations on this or the surrounding tiles. 
    /// </summary>
    public abstract class GameObject
    {
        public int x { get; protected set; }
        public int y { get; protected set; }
        public bool Blocking { get; set; } = false;
        public bool BlocksLight { get; set; } = false;
        public bool InvisibleOutsideFov { get; set; } = false;
        public string Name { get; private set; }

        // Components
        public AIComponent AIComponent { get; protected set; }
        public CombatComponent CombatComponent { get; }

        public GameObject(int x, int y, string name, AIComponent aIComponent = null, CombatComponent combatComponent = null)
        {
            this.x = x;
            this.y = y;
            Name = name;

            AIComponent = aIComponent;
            AIComponent?.SetOwner(this);

            CombatComponent = combatComponent;
            CombatComponent?.SetOwner(this);
        }

        public abstract void Render(Map map);

        public virtual void Move(int xDelta, int yDelta)
        {
            this.x += xDelta;
            this.y += yDelta;
        }
    }
}
