using System;
using BlazorRogue;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Parsed once and shared by every game: Configuration is immutable once parsed, and reads five
// JSON files off disk that would otherwise be re-read on every page load.
builder.Services.AddSingleton(_ =>
{
    var configuration = new Configuration();
    configuration.Parse();
    return configuration;
});
builder.Services.AddSingleton(TimeProvider.System);

// Holds each browser's game in memory so it survives a page reload. Constructed explicitly rather
// than by type so the DI container can't pick the tests-only constructor overload.
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

app.Run();
