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

    public List<LoginEntity> Logins { get; set; } = [];
}
