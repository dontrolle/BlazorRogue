namespace BlazorRogue.GameObjects
{
    public class FloorDecoration : GameObject
    {
        public FloorDecoration(int x, int y, string prefix, int index) : base(x, y, prefix.FirstLetterToUpperCase())
        {
            Prefix = prefix;
            Index = index;
        }

        public string Prefix { get; }
        public int Index { get; }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, $"{Prefix}_{Index}"));
        }
    }
}
