using System.Diagnostics;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// When a bot thinking about one decision must stop and play the best it has found so far (playtest 2026-11-15, D3:
/// "a timeout mechanism of approximately 30 seconds"): <see cref="Limit"/> after it began, unless it is given another.
/// </summary>
internal sealed class Deadline(TimeSpan limit)
{
    /// <summary>How long a bot may think about one decision.</summary>
    public static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    private readonly long _end = Stopwatch.GetTimestamp() + (long)(limit.TotalSeconds * Stopwatch.Frequency);

    /// <summary>Whether the time is up.</summary>
    public bool IsPast => Stopwatch.GetTimestamp() >= this._end;
}
