namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>Where one of the server's bots stands at one game.</summary>
public sealed class BotStandingEntity
{
    public string BotId { get; set; } = null!;

    public string Game { get; set; } = null!;

    public int Points { get; set; }

    public int Won { get; set; }

    public int Lost { get; set; }

    public int Drawn { get; set; }

    public int WonAgainstPeople { get; set; }

    public int LostAgainstPeople { get; set; }

    public int DrawnAgainstPeople { get; set; }
}
