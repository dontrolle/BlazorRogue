using BlazorRogue.GameObjects;

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
