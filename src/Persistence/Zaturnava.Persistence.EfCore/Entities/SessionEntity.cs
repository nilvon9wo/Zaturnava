namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>A player's session, known by its token's hash: never by the token itself.</summary>
public sealed class SessionEntity
{
    public string TokenHash { get; set; } = null!;

    public string PlayerId { get; set; } = null!;
}
