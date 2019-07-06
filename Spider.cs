using System;
using System.Collections.Generic;

namespace BlazorRogue
{
    public class Spider : Monster
    {
        public enum Type
        {
            Brown,
            BrownGiant,
            Black,
            BlackGiant,
        }

        private Dictionary<Spider.Type, string> TypeToAnimation = new Dictionary<Spider.Type, string>
        {
            {Type.Brown, "animated_brown_spider" },
            {Type.BrownGiant, "animated_spider_brown_giant" },
            {Type.Black, "animated_black_spider" },
            {Type.BlackGiant, "animated_spider_black_giant" },
        };

        public Spider(int x, int y, AIComponent aIComponent, Spider.Type type) : base(x, y, $"{type.ToString()} Spider", aIComponent, new CombatComponent(35,3,25,1,2))
        {
            base.AnimationClass = TypeToAnimation[type];
        }
    }
}
