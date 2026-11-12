namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>How one of the server's bots has done against another at a ruleset, in games played to rate them.</summary>
public sealed class BotMatchupEntity
{
    public string Ruleset { get; set; } = null!;

    /// <summary>The first bot's id, the lesser of the two (ordinal); the counts are its own.</summary>
    public string First { get; set; } = null!;

    public string Second { get; set; } = null!;

    public int Won { get; set; }

    public int Lost { get; set; }

    public int Drawn { get; set; }
}
