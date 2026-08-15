using System.Collections.Generic;
using BlazorRogue.Entities;

namespace BlazorRogue.Tests;

public class SettingsMapTests
{
    [Fact]
    public void GetWeightedIdsReturnsTheStoredWeightedIdList()
    {
        var settings = new SettingsMap(
            new Dictionary<string, object>
            {
                ["wall_tile_set"] = new List<(string, double)> { ("crypt", 1.0), ("dungeon", 2.0) },
            }
        );

        var weights = settings.GetWeightedIds("wall_tile_set", []);

        Assert.Equal([("crypt", 1.0), ("dungeon", 2.0)], weights);
    }

    [Fact]
    public void GetWeightedIdsReturnsDefaultValueWhenKeyIsMissing()
    {
        var weights = SettingsMap.Empty.GetWeightedIds("wall_tile_set", [("fallback", 1.0)]);

        Assert.Equal([("fallback", 1.0)], weights);
    }
}
