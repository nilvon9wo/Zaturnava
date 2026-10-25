using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Running;

/// <summary>
/// When each game's current decision began, as far as this host has seen: the first time it saw the
/// game at its version. Kept in memory, one per host, so a restart gives every timed decision its
/// full time again.
/// </summary>
public sealed class DecisionClock(TimeProvider time)
{
    private readonly TimeProvider _time = time;
    private readonly ConcurrentDictionary<GameId, (int Version, DateTimeOffset Since)> _seen = new();

    public DateTimeOffset Now => this._time.GetUtcNow();

    /// <summary>When the game was first seen at <paramref name="version"/>: now, if it has not been.</summary>
    public DateTimeOffset Since([Required] GameId game, int version) =>
        this._seen.AddOrUpdate(
            game,
            _ => (version, this.Now),
            (_, seen) => seen.Version == version
                ? seen
                : (version, this.Now)
        ).Since;
}
