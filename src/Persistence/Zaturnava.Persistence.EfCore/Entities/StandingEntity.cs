namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>Where a player stands at one game: let go with the player.</summary>
public sealed class StandingEntity
{
    public string PlayerId { get; set; } = null!;

    public string Game { get; set; } = null!;

    public double Elo { get; set; }

    public int Experience { get; set; }

    public int Points { get; set; }

    public int Won { get; set; }

    public int Lost { get; set; }

    public int Drawn { get; set; }

    public int WonAgainstBots { get; set; }

    public int LostAgainstBots { get; set; }

    public int DrawnAgainstBots { get; set; }
}
