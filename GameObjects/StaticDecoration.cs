using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorRogue.GameObjects
{
    public class StaticDecoration : GameObject
    {
        public override string? InfoText => infoText;

        private readonly string png;
        private readonly string? infoText;

        public StaticDecoration(int x, int y, string name, string png, string? infoText = null) : base(x, y, name)
        {
            this.png = png;
            this.infoText = infoText;
        }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, png));
        }
    }
}
