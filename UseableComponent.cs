using System;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.GameObjects;

namespace BlazorRogue
{
  public class UseableComponent(Action<GameObject> onUse) : Component
  {
    private readonly Action<GameObject> onUse = onUse;

    public void Use()
    {
      onUse.Invoke(Owner!);
    }
  }
}
