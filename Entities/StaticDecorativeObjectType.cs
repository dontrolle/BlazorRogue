using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorRogue.Entities
{
    public class StaticDecorativeObjectType
    {
        // TODO: Bad idea to have random objects all over; collect into one, to able to control
        private readonly Random random = new Random();

        public string Id { get; }
        public string Name { get; }
        private readonly List<string> imageVariants;
        public IEnumerable<string> ImageVariants => imageVariants.AsReadOnly();

        public string InfoText { get; }
        public int VerticalOffset { get; }
        public string Character { get; }
        public string CharacterColor { get; }

        public StaticDecorativeObjectType(string id, string name, List<string> image, string infoText, int verticalOffset, string character, string characterColor)
        {
            Id = id;
            Name = name;
            imageVariants = image;
            InfoText = infoText;
            VerticalOffset = verticalOffset;
            Character = character;
            CharacterColor = characterColor;
        }

        public int RandomImageVariantIndex => random.Next(0, imageVariants.Count - 1);

        public string RandomImage => imageVariants.ElementAt(RandomImageVariantIndex);
    }
}
