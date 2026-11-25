using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// The leaderboards as last counted, each kept for <see cref="Fresh"/> and given again meanwhile, so that the lobby
/// need not wait on a count that replays every game waiting on a player (the user's report of 2026-11-25: the lobby
/// slow to load, the ratings to be cached). Forgotten whenever games are newly rated, so that a game finished shows
/// at once.
/// </summary>
public sealed class StandingCache(TimeProvider time)
{
    /// <summary>How long a leaderboard is given as counted (Proposed): a game abandoned shows within it.</summary>
    public static readonly TimeSpan Fresh = TimeSpan.FromMinutes(1);

    private readonly TimeProvider _time = time;
    private readonly ConcurrentDictionary<string, Kept> _kept = new(StringComparer.Ordinal);

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

    /// <summary>Every leaderboard forgotten, to be counted anew when next asked for.</summary>
    public void Forget() => this._kept.Clear();

    private sealed record Kept(DateTimeOffset At, object Value);
}
