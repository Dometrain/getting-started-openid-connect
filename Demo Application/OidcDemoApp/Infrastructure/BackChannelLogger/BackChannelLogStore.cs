using System.Diagnostics;

namespace OidcDemoApp.Infrastructure.BackChannelLogger;


// ── Static in-memory store (shared by all handlers) ───────────────────────

public static class BackChannelLogStore
{
    private static readonly List<BackChannelLogEntry> _entries = [];
    private static readonly Lock _lock = new();

    // Shared timeline so OIDC and Flurl calls appear on the same clock
    private static readonly Stopwatch _globalSw = new();
    private static TimeSpan? _lastRequestTime;

    public static IReadOnlyList<BackChannelLogEntry> Entries
    {
        get { lock (_lock) { return _entries.ToList().AsReadOnly(); } }
    }

    public static int Count
    {
        get { lock (_lock) { return _entries.Count; } }
    }

    public static void Add(BackChannelLogEntry entry)
    {
        lock (_lock) { _entries.Add(entry); }
    }

    public static void Clear()
    {
        lock (_lock) { _entries.Clear(); }
    }

    /// <summary>Returns (elapsedSinceStart, elapsedSincePrevious) and advances the timeline.</summary>
    public static (TimeSpan ElapsedSinceStart, TimeSpan? ElapsedSincePrevious) GetTimings()
    {
        lock (_lock)
        {
            if (!_globalSw.IsRunning) _globalSw.Start();

            var elapsed = _globalSw.Elapsed;
            var diff = _lastRequestTime.HasValue ? elapsed - _lastRequestTime.Value : (TimeSpan?)null;
            _lastRequestTime = elapsed;
            return (elapsed, diff);
        }
    }
}
