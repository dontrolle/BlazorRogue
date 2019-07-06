using System;

namespace BlazorRogue
{
    public class Goblin : Monster
    {
        public Goblin(int x, int y, AIComponent aIComponent) : base(x, y, "Goblin", aIComponent, new CombatComponent(25, 7, 30, 1, 11), "animated_goblin")
        {
        }
    }
}
