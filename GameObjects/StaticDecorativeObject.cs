using System;
using System.Linq;
using BlazorRogue.Entities;

namespace BlazorRogue.GameObjects
{
    public class StaticDecorativeObject : GameObject
    {
        public override string? InfoText => infoText;

        private readonly string image;
        private readonly string? infoText;
        private readonly int verticalOffset;
        private readonly string character;
        private readonly string characterColor;

        public StaticDecorativeObject(
            int x, 
            int y, 
            StaticDecorativeObjectType staticDecorativeObjectType, 
            int? imageIndex = null, 
            int? verticalOffsetOverride = null,
            string? nameOverride = null,
            string? infoTextOverride = null) : base(x, y, nameOverride ?? staticDecorativeObjectType.Name)
        {
            if(imageIndex != null)
            {
                if(imageIndex < 0 || imageIndex >= staticDecorativeObjectType.ImageVariants.Count())
                {
                    throw new ArgumentException($"{nameof(imageIndex)} must be an index into {nameof(staticDecorativeObjectType.ImageVariants)}, i.e., be between 0 and the length-1 of that collection.");
                }

                image = staticDecorativeObjectType.ImageVariants.ElementAt(imageIndex.Value);
            }
            else
            {
                // if no index is given, select a random image among the variants given
                image = staticDecorativeObjectType.RandomImage;
            }
            
            infoText = infoTextOverride ?? staticDecorativeObjectType.InfoText;
            verticalOffset = verticalOffsetOverride?? staticDecorativeObjectType.VerticalOffset;
            character = staticDecorativeObjectType.Character;
            characterColor = staticDecorativeObjectType.CharacterColor;
        }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, image) { VerticalOffset = verticalOffset, Character = character, CharacterColor = characterColor });
        }
    }
}
