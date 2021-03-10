using System;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.Entities;

namespace BlazorRogue.GameObjects
{
    public class StaticDecorativeObject : GameObject
    {
        public override string? InfoText => infoText;

        private readonly string png;
        private readonly string? infoText;
        private readonly int verticalOffset;
        private readonly string character;
        private readonly string characterColor;

        public StaticDecorativeObject(int x, int y, string name, string png, string? infoText = null, int offset = 0, string character = "", string characterColor = "purple") : base(x, y, name)
        {
            this.png = png;
            this.infoText = infoText;
            verticalOffset = offset;
            this.character = character;
            this.characterColor = characterColor;
        }

        public StaticDecorativeObject(int x, int y, StaticDecorativeObjectType staticDecorativeObjectType) : base(x, y, staticDecorativeObjectType.Name)
        {
            png = staticDecorativeObjectType.Image;
            infoText = staticDecorativeObjectType.InfoText;
            verticalOffset = staticDecorativeObjectType.VerticalOffset;
            character = staticDecorativeObjectType.Character;
            characterColor = staticDecorativeObjectType.CharacterColor;
        }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, png) { VerticalOffset = verticalOffset, Character = character, CharacterColor = characterColor });
        }
    }
}
