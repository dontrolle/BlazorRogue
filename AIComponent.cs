using System;

namespace BlazorRogue
{
    public abstract class AIComponent : Component
    {
        protected readonly Map Map;

        public abstract void TakeTurn();

        public AIComponent(Map map) : base()
        {
            this.Map = map;
        }
    }
}
