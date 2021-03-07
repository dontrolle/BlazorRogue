﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorRogue.GameObjects
{
    public class StaticDecorativeObject : GameObject
    {
        public override string? InfoText => infoText;

        private readonly string png;
        private readonly string? infoText;
        private readonly int offset;

        public StaticDecorativeObject(int x, int y, string name, string png, string? infoText = null, int offset = 0) : base(x, y, name)
        {
            this.png = png;
            this.infoText = infoText;
            this.offset = offset;
        }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, png) { Offset = offset });
        }
    }
}
