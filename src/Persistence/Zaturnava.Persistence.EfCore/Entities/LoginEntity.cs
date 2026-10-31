namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>A player's id at a sign-in provider: one player's alone, so the two together are its key.</summary>
public sealed class LoginEntity
{
    public string Provider { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string PlayerId { get; set; } = null!;
}
