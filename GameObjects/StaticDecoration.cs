using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorRogue.GameObjects
{
    public class StaticDecoration : GameObject
    {
        private readonly string png;

        public StaticDecoration(int x, int y, string name, string png) : base(x, y, name)
        {
            this.png = png;
        }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, png));
        }
    }
}
