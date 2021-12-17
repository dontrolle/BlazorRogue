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
        private readonly Dictionary<string, string> imageVariants;
        public IReadOnlyDictionary<string, string> ImageVariants => imageVariants;

        public string InfoText { get; }
        public int VerticalOffset { get; }
        public string Character { get; }
        public string CharacterColor { get; }
        public bool Blocking { get; }
        public string ImgFolder { get; }

        public StaticDecorativeObjectType(string id, string name, Dictionary<string, string> image, string infoText, int verticalOffset, string character, string characterColor, bool blocking, string imgFolder)
        {
            Id = id;
            Name = name;
            imageVariants = image;
            InfoText = infoText;
            VerticalOffset = verticalOffset;
            Character = character;
            CharacterColor = characterColor;
            Blocking = blocking;
            ImgFolder = imgFolder;
        }

        private int RandomImageVariantIndex => random.Next(0, imageVariants.Count - 1);

        public string RandomImage => imageVariants.ElementAt(RandomImageVariantIndex).Value;
    }
}
