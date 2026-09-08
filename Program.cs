using System;
using System.IO;
using System.Linq;
using BlazorRogue;
using BlazorRogue.Entities;
using BlazorRogue.Rendering;
using BlazorRogue.Sessions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Parsed once and shared by every game: Configuration is immutable once parsed, and reads JSON
// files off disk that would otherwise be re-read on every page load.
builder.Services.AddSingleton(_ =>
{
    var configuration = new Configuration();
    configuration.Parse();
    return configuration;
});
builder.Services.AddSingleton(TimeProvider.System);

// The licensed tileset's atlas (see tools/AtlasPacker) - never checked into source control.
// BLAZORROGUE_ART_PATH points at the deployed art bundle; when unset (any contributor machine
// without a license, and CI), SpriteAtlas.IsAvailable is false and the game falls back to the
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

// Streams one obfuscated sheet's raw (still-masked) bytes; unmasking happens client-side in
// wwwroot/atlas.js. `file` is validated against the atlas's own known sheet files rather than
// trusted as a path fragment - it never leaves this directory regardless, but this also means a
// request for anything else in the art path (or an art path that happens to hold more than sheet
// bundles) gets a 404, not a 200.
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
