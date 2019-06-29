using System;

public class SpiderWeb : GameObject
{
    private int SpiderwebIndex;

    public SpiderWeb(int x, int y, int spiderwebIndex) : base(x,y)
    {
        SpiderwebIndex = spiderwebIndex;
    }

    public override void Render(Map map)
    {
        map.Decorations[x,y].Add(new Decoration(this, "web_" + SpiderwebIndex));
    }
}