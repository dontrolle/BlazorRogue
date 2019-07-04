using System;

namespace BlazorRogue
{
    public abstract class AIComponent : Component
    {
        protected readonly Map Map;
        public bool Awake { get; protected set; }

        public abstract void TakeTurn();

        public void Wake()
        {
            Awake = true;
        }

        public AIComponent(Map map) : base()
        {
            this.Map = map;
        }
    }
}
