using System;
using BlazorRogue.GameObjects;

namespace BlazorRogue;

class Decoration(GameObject gameObject, string? imageName, string imageFolder = "uf_terrain")
{
    internal enum Layer
    {
        Infront,
        Middleground,
        Behind,
    }

    public GameObject GameObject { get; private set; } = gameObject;
    public string? ImageName { get; private set; } = imageName;
    public string ImageFolder { get; private set; } = imageFolder;
    public string? AnimationClass { get; set; }

    /// <summary>
    /// Freezes <see cref="AnimationClass"/> on a single frame, for things that are still drawn but
    /// no longer moving - a dead player's corpse, for instance.
    /// </summary>
    public bool AnimationPaused { get; set; }
    public Action? OnUse { get; set; }
    public int VerticalOffset { get; set; }
    public int HorizontalOffset { get; set; }
    public bool MakeCoveringOffsetDecsTransparent { get; set; }

    public Layer DecorationLayer { get; set; } = Layer.Middleground;

    public bool BlocksLight => GameObject.BlocksLight;
    public bool Shake { get; set; }

    public string Character
    {
        get => (field) ?? "";
        set;
    }

    public string CharacterColor { get; set; } = "orange";
}
