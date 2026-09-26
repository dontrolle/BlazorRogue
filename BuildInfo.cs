using System;

namespace BlazorRogue;

/// <summary>
/// Version and build-date shown as a small corner badge (see Shared/MainLayout.razor) so a
/// deployed instance can be identified at a glance. Populated from the BLAZORROGUE_VERSION /
/// BLAZORROGUE_BUILD_DATE environment variables that docker/Dockerfile bakes into the image;
/// both fall back to a "dev" placeholder when unset, e.g. under `dotnet run` or a plain
/// `docker build` with no build args.
/// </summary>
sealed record BuildInfo(string Version, string BuildDate)
{
    public static BuildInfo FromEnvironment(Func<string, string?> getEnvironmentVariable) =>
        new(
            getEnvironmentVariable("BLAZORROGUE_VERSION") ?? "dev",
            getEnvironmentVariable("BLAZORROGUE_BUILD_DATE") ?? "unknown"
        );
}
