using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// One-time codes, each letting the window a player opens to sign in with another service add that service's login
/// to them (<see cref="PlayerService.Link"/>), rather than sign in someone new; so that the player's own token never
/// goes into an address. A code lasts <see cref="Lifetime"/>, and is gone once used. Only its hash is kept, and only
/// in memory: a host started again has forgotten them all, and the player need only ask again.
/// </summary>
public sealed class LinkCodes(TimeProvider time)
{
    /// <summary>How long a code may wait to be used.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly TimeProvider _time = time;
    private readonly ConcurrentDictionary<string, (PlayerId Player, DateTimeOffset End)> _codes =
        new(StringComparer.Ordinal);

    /// <summary>A new code for <paramref name="player"/>; any code whose time is up is let go meanwhile.</summary>
    public string Issue(PlayerId player)
    {
        DateTimeOffset now = this._time.GetUtcNow();
        foreach (KeyValuePair<string, (PlayerId Player, DateTimeOffset End)> held in this._codes)
        {
            if (held.Value.End <= now)
            {
                _ = this._codes.TryRemove(held);
            }
        }

        string code = SeatTokens.Issue();
        this._codes[SeatTokens.Hash(code)] = (player, now + Lifetime);
        return code;
    }

    /// <summary>The player the code was issued to, if it was, and its time is not up; either way, it is gone.</summary>
    public PlayerId? Redeem([Required] string code) =>
        this._codes.TryRemove(SeatTokens.Hash(code), out (PlayerId Player, DateTimeOffset End) held)
        && held.End > this._time.GetUtcNow()
            ? held.Player
            : null;
}
