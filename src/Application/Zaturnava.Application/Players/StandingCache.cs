using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// The leaderboards as last read, each kept for <see cref="Fresh"/> and given again meanwhile (the user's report of
/// 2026-11-25: the lobby slow to load, the ratings to be cached), and the games abandoned as last counted in the
/// background, so that a leaderboard never waits on a game (the user, 2026-11-26: eventual consistency is fine).
/// Forgotten whenever games are newly rated, so that a game finished shows at once.
/// </summary>
public sealed class StandingCache(TimeProvider time)
{
    /// <summary>How long a leaderboard is given as counted (Proposed): a game abandoned shows within it.</summary>
    public static readonly TimeSpan Fresh = TimeSpan.FromMinutes(1);

    private readonly TimeProvider _time = time;
    private readonly ConcurrentDictionary<string, Kept> _kept = new(StringComparer.Ordinal);
    private volatile ImmutableDictionary<string, ImmutableDictionary<PlayerId, int>> _abandoned = [];

    /// <summary>What <paramref name="key"/> names, as last counted if fresh, else counted anew and kept.</summary>
    public async Task<T> Of<T>([Required] string key, [Required] Func<Task<T>> count)
        where T : class
    {
        DateTimeOffset now = this._time.GetUtcNow();
        if (this._kept.TryGetValue(key, out Kept? kept) && now - kept.At < Fresh && kept.Value is T value)
        {
            return value;
        }

        T counted = await count();
        this._kept[key] = new Kept(now, counted);
        return counted;
    }

    /// <summary>When the games abandoned were last counted, if ever.</summary>
    public DateTimeOffset? AbandonedCountedAt { get; private set; }

    /// <summary>Every leaderboard forgotten, to be counted anew when next asked for.</summary>
    public void Forget() => this._kept.Clear();

    /// <summary>
    /// How many games of <paramref name="game"/> each player has abandoned, as last counted in the background
    /// (<see cref="StandingService.CountAbandoned"/>): none, until first counted.
    /// </summary>
    public ImmutableDictionary<PlayerId, int> AbandonedAt([Required] string game) =>
        this._abandoned.GetValueOrDefault(game) ?? [];

    /// <summary>The games abandoned, as just counted, kept for every leaderboard asked for after.</summary>
    public void KeepAbandoned([Required] ImmutableDictionary<string, ImmutableDictionary<PlayerId, int>> abandoned)
    {
        this._abandoned = abandoned;
        this.AbandonedCountedAt = this._time.GetUtcNow();
    }

    private sealed record Kept(DateTimeOffset At, object Value);
}
