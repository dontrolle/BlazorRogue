namespace BlazorRogue.Entities
{
    public class StaticDecorativeObjectType
    {
        public string Id { get; }
        public string Name { get; }
        public string Image { get; }
        public string InfoText { get; }
        public int VerticalOffset { get; }
        public string Character { get; }
        public string CharacterColor { get; }

        public StaticDecorativeObjectType(string id, string name, string image, string infoText, int verticalOffset, string character, string characterColor)
        {
            Id = id;
            Name = name;
            Image = image;
            InfoText = infoText;
            VerticalOffset = verticalOffset;
            Character = character;
            CharacterColor = characterColor;
        }
    }
}
