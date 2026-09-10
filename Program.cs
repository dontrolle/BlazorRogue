using System;
using System.IO;
using System.Linq;
using BlazorRogue;
using BlazorRogue.Entities;
using BlazorRogue.Rendering;
using BlazorRogue.Sessions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server circuits are stateful and GameSessionStore is in-process, so the app runs as a
// single instance regardless of where it is hosted. A disconnected circuit pins its render tree in
// memory until it is reclaimed or evicted, so under a burst of visitors the retained-circuit cap is
// the memory knob that matters. Cutting it well below the framework default (100 / 3 min) is cheap
// here because the game itself lives in GameSessionStore keyed by a localStorage id (see
// Sessions/GameSessionStore.cs) - when a circuit is evicted, a full page reload resumes the same
// game rather than a seamless reconnect.
builder
    .Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DisconnectedCircuitMaxRetained = 30;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(2);
    });

// When the app runs behind a reverse proxy that terminates TLS, it only ever sees plain HTTP.
// Honour X-Forwarded-Proto/-For so app.UseHttpsRedirection() below sees the original "https" scheme
// (without this it redirect-loops) and so logs record the real client IP. The known-proxy /
// known-network allowlists are cleared rather than pinned because the proxy's address is not
// statically known; this is safe only when the app's own port is not reachable from outside,
// leaving the proxy as the only thing that can set these headers. If the app is ever exposed
// directly, pin KnownProxies instead.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Parsed once and shared by every game: Configuration is immutable once parsed, and reads JSON
// files off disk that would otherwise be re-read on every page load.
builder.Services.AddSingleton(_ =>
{
    var configuration = new Configuration();
    configuration.Parse();
    return configuration;
});
builder.Services.AddSingleton(TimeProvider.System);

// The tileset atlas (see tools/AtlasPacker) - never checked into source control.
// BLAZORROGUE_ART_PATH points at the deployed art bundle; when unset (any contributor machine
// without the tileset, and CI), SpriteAtlas.IsAvailable is false and the game falls back to the
// ASCII renderer, same as when the loose tileset files used to be absent.
string? artPath = Environment.GetEnvironmentVariable("BLAZORROGUE_ART_PATH");
builder.Services.AddSingleton(_ => SpriteAtlas.Load(artPath));

// Holds each browser's game in memory so it survives a page reload. Constructed explicitly rather
// than by type so the DI container can't pick the tests-only constructor overload. Which level a
// new game starts on is driven by Data/game-config.json's "starting_level" (see Configuration).
builder.Services.AddSingleton(sp => new GameSessionStore(
    sp.GetRequiredService<Configuration>(),
    sp.GetRequiredService<TimeProvider>()
));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
#pragma warning disable CA1303 // Do not pass literals as localized parameters
    Console.WriteLine("IsDevelopment==true");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
    _ = app.UseDeveloperExceptionPage();
    _ = app.UseBrowserLink();
}
else
{
#pragma warning disable CA1303 // Do not pass literals as localized parameters
    Console.WriteLine("IsDevelopment==false");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
    _ = app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

// Ahead of UseHttpsRedirection and routing, so everything downstream sees the caller's real scheme
// and IP when the app sits behind a proxy.
app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Generated once from the (immutable, already-parsed) Configuration/SpriteAtlas singletons and
// cached for the life of the process: one .spr-<key> class per atlas sprite (World/Tile.cs,
// GamePage.razor's decoration/item/HUD rendering) plus every @keyframes animation, rather than
// hand-duplicating a block per hero/monster/liquid/torch in a static stylesheet.
var generatedCss = new Lazy<string>(() =>
{
    var configuration = app.Services.GetRequiredService<Configuration>();
    var atlas = app.Services.GetRequiredService<SpriteAtlas>();
    return atlas.GenerateStaticCss()
        + AnimationCssGenerator.Generate(
            configuration.HeroTypes.Values.Concat(configuration.MonsterTypes.Values),
            atlas
        )
        + AnimationCssGenerator.Generate(configuration.LiquidTypes, atlas)
        + HandAuthoredSpriteAnimations.Generate(atlas);
});
app.MapGet("/css/generated-animations.css", () => Results.Text(generatedCss.Value, "text/css"));

// Streams one obfuscated sheet's raw bytes; the browser reverses that in wwwroot/atlas.js.
// `file` is validated against the atlas's own known sheet files rather than trusted as a path
// fragment - a request for anything else in the art path gets a 404, not a 200.
app.MapGet(
    "/a/{file}",
    (string file, HttpContext context, SpriteAtlas atlas) =>
    {
        if (artPath is null || !atlas.IsKnownSheetFile(file))
        {
            return Results.NotFound();
        }

        // Immutable: a sheet's content only ever changes by a fresh deploy, which serves it under
        // whatever new file name the next atlas-pack run assigns (see tools/AtlasPacker).
        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return Results.File(Path.Combine(artPath, file), "application/octet-stream");
    }
);

app.Run();
