namespace BlazorRogue.Tests;

public class BuildInfoTests
{
    [Fact]
    public void FromEnvironmentReadsBothVariables()
    {
        var buildInfo = BuildInfo.FromEnvironment(name =>
            name switch
            {
                "BLAZORROGUE_VERSION" => "1.2.3",
                "BLAZORROGUE_BUILD_DATE" => "2026-09-26T12:00:00Z",
                _ => null,
            }
        );

        Assert.Equal("1.2.3", buildInfo.Version);
        Assert.Equal("2026-09-26T12:00:00Z", buildInfo.BuildDate);
    }

    [Fact]
    public void FromEnvironmentFallsBackWhenVariablesAreUnset()
    {
        var buildInfo = BuildInfo.FromEnvironment(_ => null);

        Assert.Equal("dev", buildInfo.Version);
        Assert.Equal("unknown", buildInfo.BuildDate);
    }
}
