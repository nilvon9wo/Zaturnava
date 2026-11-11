namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// The aliases guests have taken for their visits (Stated 2026-11-11): each held by one token, only its hash kept,
/// until <see cref="Kept"/> passes without its holder renewing it, alike whatever its case. Kept in memory: a guest's
/// visit does not outlive the server, and a guest renews theirs as the page opens.
/// </summary>
public sealed class GuestAliases(TimeProvider clock)
{
    /// <summary>How long an alias is held without its holder renewing it (Proposed).</summary>
    public static readonly TimeSpan Kept = TimeSpan.FromHours(12);

    private readonly TimeProvider _clock = clock;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, Holding> _held = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether anyone but the holder of <paramref name="hash"/>, if given, holds <paramref name="alias"/>.
    /// </summary>
    public bool IsHeld(string alias, string? hash = null)
    {
        lock (this._lock)
        {
            return this.IsHeldNow(alias, hash);
        }
    }

    /// <summary>
    /// Holds <paramref name="alias"/> for the holder of <paramref name="hash"/>, letting go whatever else they held;
    /// or, if someone else holds it, nothing, and false.
    /// </summary>
    public bool Hold(string alias, string hash)
    {
        lock (this._lock)
        {
            if (this.IsHeldNow(alias, hash))
            {
                return false;
            }

            DateTimeOffset now = this._clock.GetUtcNow();
            foreach (string gone in this._held
                .Where(held => held.Value.Hash == hash || held.Value.Until <= now)
                .Select(held => held.Key)
                .ToList())
            {
                _ = this._held.Remove(gone);
            }

            this._held[alias] = new Holding(hash, now + Kept);
            return true;
        }
    }

    /// <summary>
    /// The alias the holder of <paramref name="hash"/> holds, held anew for as long; none, if they hold none.
    /// </summary>
    public string? Renew(string hash)
    {
        lock (this._lock)
        {
            DateTimeOffset now = this._clock.GetUtcNow();
            string? alias = this._held
                .Where(held => held.Value.Hash == hash && held.Value.Until > now)
                .Select(held => held.Key)
                .FirstOrDefault();
            if (alias is not null)
            {
                this._held[alias] = new Holding(hash, now + Kept);
            }

            return alias;
        }
    }

    private bool IsHeldNow(string alias, string? hash) =>
        this._held.TryGetValue(alias, out Holding? held)
        && held.Until > this._clock.GetUtcNow()
        && held.Hash != hash;

    private sealed record Holding(string Hash, DateTimeOffset Until);
}
