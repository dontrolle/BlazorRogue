using System;
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

// Which level new sessions start on - normally unset (level 0). Set "Game:StartingLevelId" (env
// var Game__StartingLevelId, or appsettings) to a level's id, e.g. "test_level", to make every new
// session start there instead - see the "BlazorRogue (Test Level)" launch profile.
string? startingLevelId = builder.Configuration["Game:StartingLevelId"];

// Holds each browser's game in memory so it survives a page reload. Constructed explicitly rather
// than by type so the DI container can't pick the tests-only constructor overload.
builder.Services.AddSingleton(sp => new GameSessionStore(
    sp.GetRequiredService<Configuration>(),
    sp.GetRequiredService<TimeProvider>(),
    startingLevelId
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

// Generated once from the (immutable, already-parsed) Configuration singleton and cached for the
// life of the process, rather than hand-duplicating one @keyframes block per hero/monster in
// wwwroot/css/animations.css.
var generatedAnimationsCss = new Lazy<string>(() =>
{
    var configuration = app.Services.GetRequiredService<Configuration>();
    return AnimationCssGenerator.Generate(
            configuration.HeroTypes.Values.Concat(configuration.MonsterTypes.Values)
        ) + AnimationCssGenerator.Generate(configuration.LiquidTypes);
});
app.MapGet(
    "/css/generated-animations.css",
    () => Results.Text(generatedAnimationsCss.Value, "text/css")
);

app.Run();
