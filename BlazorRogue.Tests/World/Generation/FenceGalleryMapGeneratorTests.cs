using System;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation;

/// <summary>
/// A regression guard for the fence gallery debug level (see Game.ToggleFenceGalleryDebugView,
/// dontrolle/BlazorRogue-internal#86): as the fence catalog grows, a new type that makes
/// FenceGalleryMapGenerator run off the map edge or throw should fail here rather than only being
/// noticed the next time someone actually looks at the gallery in-browser.
/// </summary>
public class FenceGalleryMapGeneratorTests
{
    static LevelConfiguration GalleryLevel() =>
        new(
            number: 0,
            id: "fence-gallery-test-level",
            name: "Fence Gallery Test Level",
            height: 20,
            width: 30,
            generatorId: FenceGalleryMapGenerator.Id,
            backgroundSoundtrack: "test.mp3",
            settingsMap: SettingsMap.Empty
        );

    [Fact]
    public void GeneratesEveryCurrentFenceTypeAtLeastOnce()
    {
        var game = new Game();
        var map = MapGeneratorFactory.Create(GalleryLevel(), game).GenerateMap();

        // Several fence_* types share a display Name ("Fence"), so identify placements by their
        // rendered image instead - each type has a distinct one.
        var renderedImages = new HashSet<string?>();
        map.ForEachTile(
            (x, y) =>
            {
                foreach (var decoration in map.Decorations[x, y])
                {
                    renderedImages.Add(decoration.ImageName);
                }
            }
        );

        var everyFenceType = game.Configuration.StaticDecorativeObjectTypes.Values.Where(t =>
            t.Id.StartsWith("fence_", StringComparison.Ordinal)
        );

        Assert.All(everyFenceType, t => Assert.Contains(t.RandomImage, renderedImages));
    }
}
