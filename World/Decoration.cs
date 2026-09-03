using System;
using BlazorRogue.GameObjects;

namespace BlazorRogue.World;

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

    /// <summary>
    /// Clockwise rotation in degrees (0/90/180/270) applied to the rendered sprite, and an optional
    /// horizontal flip. Used to build the water-pool shoreline from a handful of canonically-oriented
    /// <c>water_edging_*</c> tiles (see <see cref="Tile"/>) without needing a pre-rotated asset per
    /// direction. Composes after VerticalOffset/HorizontalOffset, which liquid edging never uses.
    /// </summary>
    public int RotationDegrees { get; set; }
    public bool MirrorX { get; set; }
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
