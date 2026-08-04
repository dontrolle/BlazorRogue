using BlazorRogue.Tests.TestSupport;

namespace BlazorRogue.Tests;

/// <summary>
/// Covers the session bookkeeping that lets a game survive a page reload: the same browser must get
/// its game back, different browsers must stay isolated (the engine reaches game state through the
/// static <see cref="References"/> hub, so a leak here would corrupt both games), and sessions must
/// not accumulate in memory indefinitely.
/// </summary>
public class GameSessionStoreTests
{
    // Parsing configuration reads the JSON data files, and every session generates a dungeon, so
    // tests share one parsed configuration and keep the session limits small.
    static Configuration ParsedConfiguration()
    {
        var configuration = new Configuration();
        configuration.Parse();
        return configuration;
    }

    static GameSessionStore CreateStore(
        FakeTimeProvider timeProvider,
        int maxSessions = 3,
        TimeSpan? idleTimeout = null
    ) =>
        new(ParsedConfiguration(), timeProvider, maxSessions, idleTimeout ?? TimeSpan.FromHours(2));

    [Fact]
    public void SameIdResumesTheSameGame()
    {
        var store = CreateStore(new FakeTimeProvider());

        var first = store.GetOrCreate("browser-a");
        var second = store.GetOrCreate("browser-a");

        // Reference equality is the point: a reload must return the very same Game, not an
        // equivalent one, or the dungeon and the player's progress would be regenerated.
        Assert.Same(first, second);
        Assert.Same(first.Game, second.Game);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void DifferentIdsGetIndependentGames()
    {
        var store = CreateStore(new FakeTimeProvider());

        var a = store.GetOrCreate("browser-a");
        var b = store.GetOrCreate("browser-b");

        Assert.NotSame(a, b);
        Assert.NotSame(a.Game, b.Game);
        Assert.NotSame(a.Game.Map, b.Game.Map);
        Assert.NotSame(a.Game.Map.Player, b.Game.Map.Player);
        Assert.Equal(2, store.Count);
    }

    [Fact]
    public void ConfigurationIsSharedBetweenSessions()
    {
        var store = CreateStore(new FakeTimeProvider());

        var a = store.GetOrCreate("browser-a");
        var b = store.GetOrCreate("browser-b");

        // Configuration is immutable once parsed, so re-reading the JSON data files per game would
        // be pure waste.
        Assert.Same(a.Game.Configuration, b.Game.Configuration);
    }

    [Fact]
    public void StartNewGameReplacesOnlyThatSessionsGame()
    {
        var store = CreateStore(new FakeTimeProvider());

        var session = store.GetOrCreate("browser-a");
        var untouched = store.GetOrCreate("browser-b");
        var originalGame = session.Game;
        var untouchedGame = untouched.Game;

        session.StartNewGame();

        Assert.NotSame(originalGame, session.Game);
        Assert.Same(session, store.GetOrCreate("browser-a"));
        Assert.Same(session.Game, store.GetOrCreate("browser-a").Game);
        Assert.Same(untouchedGame, untouched.Game);
    }

    [Fact]
    public void ActivatePointsTheStaticReferencesAtThatSessionsGame()
    {
        var store = CreateStore(new FakeTimeProvider());
        var soundManager = new SoundManager(new FakeJsRuntime());

        var a = store.GetOrCreate("browser-a");
        var b = store.GetOrCreate("browser-b");

        // Constructing b's game left the statics pointing at it; activating a must take them back,
        // which is what stops one player's actions landing in another player's game.
        a.Activate(soundManager);

        Assert.Same(a.Game.Map, References.Map);
        Assert.Same(a.Game.EffectsSystem, References.EffectsSystem);
        Assert.Same(a.Game.Configuration, References.Configuration);
        Assert.Same(soundManager, References.SoundManager);

        b.Activate(soundManager);

        Assert.Same(b.Game.Map, References.Map);
        Assert.Same(b.Game.EffectsSystem, References.EffectsSystem);
    }

    [Fact]
    public void IdleSessionsAreEvicted()
    {
        var time = new FakeTimeProvider();
        var store = CreateStore(time, idleTimeout: TimeSpan.FromHours(2));

        var original = store.GetOrCreate("browser-a");
        var originalGame = original.Game;

        time.Advance(TimeSpan.FromHours(3));

        // Eviction is opportunistic, so it takes another visitor to trigger the sweep.
        _ = store.GetOrCreate("browser-b");

        Assert.NotSame(originalGame, store.GetOrCreate("browser-a").Game);
    }

    [Fact]
    public void PlayingKeepsASessionAliveAcrossTheIdleTimeout()
    {
        var time = new FakeTimeProvider();
        var store = CreateStore(time, idleTimeout: TimeSpan.FromHours(2));
        var soundManager = new SoundManager(new FakeJsRuntime());

        var session = store.GetOrCreate("browser-a");
        var game = session.Game;

        // A long game touches the store only once, at load; it is Activate() on each turn that has
        // to keep the session from being reaped mid-play.
        time.Advance(TimeSpan.FromMinutes(90));
        session.Activate(soundManager);
        time.Advance(TimeSpan.FromMinutes(90));

        _ = store.GetOrCreate("browser-b");

        Assert.Same(game, store.GetOrCreate("browser-a").Game);
    }

    [Fact]
    public void SurplusSessionsAreEvictedLeastRecentlyUsedFirst()
    {
        var time = new FakeTimeProvider();
        var store = CreateStore(time, maxSessions: 3);

        var oldest = store.GetOrCreate("browser-a");
        time.Advance(TimeSpan.FromMinutes(1));
        _ = store.GetOrCreate("browser-b");
        time.Advance(TimeSpan.FromMinutes(1));

        // Re-touching the oldest session makes it the most recent, so the next one added should
        // displace "browser-b" instead.
        var refreshedGame = store.GetOrCreate("browser-a").Game;
        time.Advance(TimeSpan.FromMinutes(1));

        var newest = store.GetOrCreate("browser-c");

        Assert.True(store.Count <= 3);
        Assert.Same(refreshedGame, store.GetOrCreate("browser-a").Game);
        Assert.Same(newest.Game, store.GetOrCreate("browser-c").Game);
        Assert.Same(oldest, store.GetOrCreate("browser-a"));
    }
}
