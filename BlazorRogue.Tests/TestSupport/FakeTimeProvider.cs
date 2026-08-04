namespace BlazorRogue.Tests.TestSupport;

/// <summary>
/// A <see cref="TimeProvider"/> whose clock only moves when a test moves it, so that idle-session
/// eviction in <see cref="GameSessionStore"/> can be exercised without waiting in real time.
/// </summary>
sealed class FakeTimeProvider : TimeProvider
{
    DateTimeOffset now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => now;

    public void Advance(TimeSpan delta) => now += delta;
}
