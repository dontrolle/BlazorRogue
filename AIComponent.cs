using System;

public abstract class AIComponent {
    protected readonly Map Map;
    protected Monster Owner;

    public abstract void TakeTurn();

    public AIComponent(Map map)
    {
        this.Map = map;
    }

    public void SetOwner(Monster monster){
        Owner = monster;
    }
}