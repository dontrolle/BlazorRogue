using BlazorRogue.GameObjects;

namespace BlazorRogue.Components;

/// <summary>
/// A generic component representing a distinct and separate aspect of a <see cref="GameObject"/>.
/// </summary>
abstract class Component
{
    public GameObject? Owner { get; private set; }

    public void SetOwner(GameObject gameObject) => Owner = gameObject;
}
