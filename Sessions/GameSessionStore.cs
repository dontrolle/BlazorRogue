using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.Entities;

namespace BlazorRogue.Sessions;

/// <summary>
/// Process-wide registry of <see cref="GameSession"/>s, keyed by an id the browser keeps in
/// localStorage. Registered as a DI singleton; this is what lets a game survive a page reload,
/// since a reload starts a brand new Blazor circuit but presents the same id.
/// </summary>
/// <remarks>
/// Sessions are held in memory only: they are lost when the process restarts, and are not shared
/// between instances if the app is ever scaled out. Persisting them would need a save model rather
/// than a serialized object graph - the game graph holds delegates, events and multidimensional
/// arrays that cannot be serialized directly.
/// </remarks>
sealed class GameSessionStore
{
    /// <summary>How long a session may go untouched before it is discarded.</summary>
    public static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromHours(2);

    /// <summary>
    /// Upper bound on concurrently held sessions, so a burst of visitors cannot grow the process's
    /// memory without limit. Least recently used sessions are dropped first.
    /// </summary>
    public const int DefaultMaxSessions = 200;

    readonly ConcurrentDictionary<string, GameSession> sessions = new(StringComparer.Ordinal);
    readonly Configuration configuration;
    readonly TimeProvider timeProvider;
    readonly int maxSessions;
    readonly TimeSpan idleTimeout;

    public GameSessionStore(Configuration configuration, TimeProvider timeProvider)
        : this(configuration, timeProvider, DefaultMaxSessions, DefaultIdleTimeout) { }

    /// <summary>
    /// Overload letting tests shrink the limits, so eviction can be exercised without generating
    /// hundreds of dungeons or waiting out a real timeout.
    /// </summary>
    internal GameSessionStore(
        Configuration configuration,
        TimeProvider timeProvider,
        int maxSessions,
        TimeSpan idleTimeout
    )
    {
        this.configuration = configuration;
        this.timeProvider = timeProvider;
        this.maxSessions = maxSessions;
        this.idleTimeout = idleTimeout;
    }

    /// <summary>Number of sessions currently held. Intended for tests and diagnostics.</summary>
    public int Count => sessions.Count;

    /// <summary>
    /// Returns the session for <paramref name="id"/>, generating a dungeon for it the first time
    /// that id is seen. Callers must follow this with <see cref="GameSession.Activate"/> before
    /// running any game logic.
    /// </summary>
    public GameSession GetOrCreate(string id)
    {
        Evict();

        // The factory may run more than once under contention, but only one result is kept - two
        // browsers racing on the same id would waste a dungeon, never share a corrupted one.
        var session = sessions.GetOrAdd(
            id,
            key => new GameSession(key, configuration, timeProvider)
        );

        session.LastAccessed = timeProvider.GetUtcNow();
        return session;
    }

    /// <summary>
    /// Drops idle and surplus sessions. Called opportunistically from <see cref="GetOrCreate"/> so
    /// that no background timer is needed - an idle server simply keeps its sessions in memory
    /// until the next visitor arrives.
    /// </summary>
    void Evict()
    {
        var cutoff = timeProvider.GetUtcNow() - idleTimeout;

        foreach (var entry in sessions)
        {
            if (entry.Value.LastAccessed <= cutoff)
            {
                _ = sessions.TryRemove(entry);
            }
        }

        // Leave room for the session the caller is about to add.
        int surplus = sessions.Count - (maxSessions - 1);
        if (surplus <= 0)
        {
            return;
        }

        List<KeyValuePair<string, GameSession>> leastRecentlyUsed =
        [
            .. sessions.OrderBy(entry => entry.Value.LastAccessed).Take(surplus),
        ];

        foreach (var entry in leastRecentlyUsed)
        {
            _ = sessions.TryRemove(entry);
        }
    }
}
