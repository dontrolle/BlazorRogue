using System;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.World;

namespace BlazorRogue.Entities;

class StaticDecorativeObjectType(
    string id,
    string name,
    Dictionary<string, string> image,
    Dictionary<string, string> animationClasses,
    string infoText,
    int verticalOffset,
    string character,
    string characterColor,
    bool blocking,
    bool makeCoveringOffsetDecsTransparent,
    Edge blockedEdges = Edge.None,
    bool occupiesTile = false
)
{
    readonly Random random = new();

    public string Id { get; } = id;
    public string Name { get; } = name;
    readonly Dictionary<string, string> imageVariants = image;
    public IReadOnlyDictionary<string, string> ImageVariants => imageVariants;

    // Keyed by the same tags as imageVariants, but a tag need not appear here - most decorations
    // are static, e.g. lilypad's two tags ("a"/"b", one per lily species) each map to an animated
    // CSS class (see HandAuthoredSpriteAnimations) cycling that species' own frames, while dust's
    // six corner/straight tags map to no animation at all.
    readonly Dictionary<string, string> animationClassVariants = animationClasses;

    public string InfoText { get; } = infoText;
    public int VerticalOffset { get; } = verticalOffset;
    public string Character { get; } = character;
    public string CharacterColor { get; } = characterColor;
    public bool Blocking { get; } = blocking;
    public bool MakeCoveringOffsetDecsTransparent { get; } = makeCoveringOffsetDecsTransparent;

    /// <summary>See <see cref="Edge"/> - the "fence" edges this decoration blocks movement across.</summary>
    public Edge BlockedEdges { get; } = blockedEdges;

    /// <summary>See <see cref="GameObjects.GameObject.OccupiesTile"/>.</summary>
    public bool OccupiesTile { get; } = occupiesTile;

    string RandomTag => imageVariants.ElementAt(random.Next(0, imageVariants.Count)).Key;

    public string RandomImage => imageVariants[RandomTag];

    /// <summary>
    /// Picks a random tag and returns its image together with whatever animation class (if any)
    /// shares that tag, so a multi-frame species (e.g. lilypad's "a"/"b") never ends up with one
    /// frame's image paired with a different frame's animation.
    /// </summary>
    public (string Image, string? AnimationClass) RandomImageWithAnimationClass
    {
        get
        {
            string tag = RandomTag;
            return (imageVariants[tag], AnimationClassForTag(tag));
        }
    }

    public string? AnimationClassForTag(string tag) =>
        animationClassVariants.GetValueOrDefault(tag);
}
