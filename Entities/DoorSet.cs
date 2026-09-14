namespace BlazorRogue.Entities;

/// <summary>
/// Visual/behavioural properties for a door type (the id a <see cref="GameObjects.Door"/> is
/// constructed with, e.g. "wood" or "gate"), parsed from <c>Data/doorsets.json</c>.
/// </summary>
sealed class DoorSet(string id, string imgPrefix, bool alwaysSeeThrough, string infoText)
{
    public string Id { get; } = id;

    /// <summary>Image filename stem, e.g. <c>door_wood</c> for <c>door_wood_1.png</c> etc.</summary>
    public string ImgPrefix { get; } = imgPrefix;

    /// <summary>
    /// True for door types (e.g. a wrought-iron gate) that never block vision/light, even when
    /// closed - a closed door of this type still blocks movement, it's just not opaque. See
    /// <see cref="GameObjects.Door"/>.
    /// </summary>
    public bool AlwaysSeeThrough { get; } = alwaysSeeThrough;

    /// <summary>Base tooltip text for a door of this type - see <see cref="GameObjects.Door.InfoText"/>.</summary>
    public string InfoText { get; } = infoText;
}
