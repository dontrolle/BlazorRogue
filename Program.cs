using System;
using BlazorRogue;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

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

app.UseStaticFiles();

app.UseRouting();

app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
