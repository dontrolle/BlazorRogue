using BlazorRogue.Effects;
using Xunit;

namespace BlazorRogue.Tests;

public class EffectsSystemTests
{
    [Fact]
    public void ConsumeShakeReturnsTrueOnceThenFalse()
    {
        var effects = new EffectsSystem { Shake = true };

        Assert.True(effects.ConsumeShake());
        Assert.False(effects.ConsumeShake());
        Assert.False(effects.Shake);
    }

    [Fact]
    public void ConsumeShakeReturnsFalseWhenNoShakePending()
    {
        var effects = new EffectsSystem();

        Assert.False(effects.ConsumeShake());
    }
}
