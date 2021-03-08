using System;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.GameObjects;

namespace BlazorRogue
{
    public class UseableComponent : Component
    {
        private readonly Action<GameObject> onUse;

        public UseableComponent(Action<GameObject> onUse)
        {
            this.onUse = onUse;
        }

        public void Use()
        {
            onUse.Invoke(Owner);
        }
    }
}
