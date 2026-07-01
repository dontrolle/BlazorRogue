using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorRogue
{
  public static class References
  {
    // These are set up during Game's constructor, before any game logic runs; null! avoids
    // forcing nullable-checks throughout the codebase for values that are always non-null in practice.
    public static Map Map { get; internal set; } = null!;
    public static Configuration Configuration { get; internal set; } = null!;
    public static SoundManager SoundManager { get; internal set; } = null!;
    public static EffectsSystem EffectsSystem { get; internal set; } = null!;
  }
}
