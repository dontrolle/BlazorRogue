namespace BlazorRogue.Entities
{
    public class FloorSet
    {
        public string Id { get; }
        public string ImgPrefix { get; }
        public int[] ImgFloor { get; }
        public string CharFloor { get; }
        public string CharColor { get; }

        public FloorSet(string id, string imgPrefix, int[] imgFloor, string charFloor, string charColor)
        {
            Id = id;
            ImgPrefix = imgPrefix;
            ImgFloor = imgFloor;
            CharFloor = charFloor;
            CharColor = charColor;
        }
    }
}
