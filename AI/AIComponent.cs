using System;

namespace BlazorRogue.AI
{
  public abstract class AIComponent(Map map) : Component()
  {
    protected readonly Map Map = map;
    public bool Awake { get; protected set; }

    public abstract void TakeTurn();

    public void Wake()
    {
      Awake = true;
    }
  }
}
