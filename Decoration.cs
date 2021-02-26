using System;
using BlazorRogue.GameObjects;

namespace BlazorRogue
{
    public class Decoration
    {
        public GameObject GameObject { get; private set; }
        public string? ImageName { get; private set; }
        public string? AnimationClass { get; set; }
        public Action? OnClick { get; set; }
        public int Offset { get; set; }
        public int HOffset { get; set; }
        public bool InFront { get; set; }
        public bool BlocksLight => GameObject.BlocksLight;
        public string Character { get; set; } = "?";
        public string CharacterColor { get; set; } = "orange";

        public Decoration(GameObject gameObject, string? imageName)
        {
            GameObject = gameObject;
            ImageName = imageName;
        }
    }
}
