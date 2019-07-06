using System;

namespace BlazorRogue
{
    public class SpiderWeb : GameObject
    {
        private int SpiderwebIndex;
        public int Offset { get; set; }

        public SpiderWeb(int x, int y, int spiderwebIndex) : base(x, y, "Spider web")
        {
            SpiderwebIndex = spiderwebIndex;
        }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, "web_" + SpiderwebIndex) { Offset = Offset });
        }
    }
}
