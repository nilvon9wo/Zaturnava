namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>A player who has signed in, and the logins that are theirs.</summary>
/// <remarks>
/// Its strings are unset (null) until assigned, not defaulted, as <see cref="ActionEntity"/>'s are. The model in
/// <see cref="GameDbContext"/> keeps those columns required.
/// </remarks>
public sealed class PlayerEntity
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>When the player last signed in, in UTC; null for one not signed in since this was kept.</summary>
    public DateTime? LastSignedIn { get; set; }

    /// <summary>The page's settings the player prefers, by name, as JSON; null for none kept.</summary>
    public string? Preferences { get; set; }

    public List<LoginEntity> Logins { get; set; } = [];
}
