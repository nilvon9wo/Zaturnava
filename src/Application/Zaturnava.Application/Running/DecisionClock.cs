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
    private readonly ConcurrentDictionary<GameId, (string Wait, DateTimeOffset Since)> _waits = new();
    private readonly ConcurrentDictionary<GameId, (string Absence, DateTimeOffset Since)> _absences = new();

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

    /// <summary>
    /// When the game was first seen waiting as <paramref name="wait"/> describes (on whom, for which turn):
    /// now, if it has not been. Unlike <see cref="Since"/>, other seats acting meanwhile do not restart it.
    /// </summary>
    public DateTimeOffset WaitingSince([Required] GameId game, [Required] string wait) =>
        this._waits.AddOrUpdate(
            game,
            _ => (wait, this.Now),
            (_, seen) => seen.Wait == wait
                ? seen
                : (wait, this.Now)
        ).Since;

    /// <summary>
    /// When the game was first seen with a seat away as <paramref name="absence"/> describes (who, since acting
    /// last when): now, if it has not been. Unlike <see cref="WaitingSince"/>, the turns going round meanwhile do
    /// not restart it; only the seat acting again does, which changes the description.
    /// </summary>
    public DateTimeOffset AbsentSince([Required] GameId game, [Required] string absence) =>
        this._absences.AddOrUpdate(
            game,
            _ => (absence, this.Now),
            (_, seen) => seen.Absence == absence
                ? seen
                : (absence, this.Now)
        ).Since;
}
