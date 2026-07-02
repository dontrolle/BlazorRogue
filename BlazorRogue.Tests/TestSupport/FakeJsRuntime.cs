using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorRogue.Tests.TestSupport
{
  /// <summary>
  /// No-op <see cref="IJSRuntime"/> used so that code paths going through <see cref="SoundManager"/>
  /// (e.g. GameObject.Kill(), Moveable.Move()) can run in tests without a real Blazor JS host.
  /// </summary>
  internal sealed class FakeJsRuntime : IJSRuntime
  {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
      return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
      return ValueTask.FromResult(default(TValue)!);
    }
  }

  /// <summary>
  /// Ensures BlazorRogue's static <see cref="References"/> hub has a working (fake)
  /// <see cref="SoundManager"/> before any test runs, since many game classes reach it directly
  /// rather than through DI (see GameObject.Kill(), Moveable.Move()).
  /// </summary>
  internal static class AssemblySetup
  {
    [ModuleInitializer]
    public static void Initialize()
    {
      References.SoundManager = new SoundManager(new FakeJsRuntime());
    }
  }
}
