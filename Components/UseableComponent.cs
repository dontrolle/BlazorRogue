using System;
using BlazorRogue.GameObjects;

namespace BlazorRogue.Components;

class UseableComponent(Action<GameObject> onUse) : Component
{
    readonly Action<GameObject> onUse = onUse;

    public void Use() => onUse.Invoke(Owner!);
}
