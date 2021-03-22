﻿using System;
using BlazorRogue.GameObjects;

namespace BlazorRogue
{
    public class Decoration
    {
        public GameObject GameObject { get; private set; }
        public string? ImageName { get; private set; }
        public string? AnimationClass { get; set; }
        public Action? OnClick { get; set; }
        public int VerticalOffset { get; set; }
        public int HorizontalOffset { get; set; }
        public bool InFront { get; set; }
        public bool BlocksLight => GameObject.BlocksLight;

        private const string DefaultCharacter = "?";
        private string? character;

        public string Character { 
            get
            {
                if (character != null)
                {
                    return character;
                }
                else if(VerticalOffset == 0 && HorizontalOffset == 0)
                {
                    return DefaultCharacter;
                }

                // in ascii-mode, we very seldomly want any of the out-of-tile-decorations to show
                return "";
            }
            set
            {
                character = value;
            }
        }

        public string CharacterColor { get; set; } = "orange";

        public Decoration(GameObject gameObject, string? imageName)
        {
            GameObject = gameObject;
            ImageName = imageName;
        }
    }
}
