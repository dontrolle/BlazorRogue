using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorRogue
{
    public abstract class Component
    {
        public GameObject Owner { get; private set; }

        public void SetOwner(GameObject gameObject)
        {
            Owner = gameObject;
        }
    }
}
