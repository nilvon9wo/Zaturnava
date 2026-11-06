namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// A player on a leaderboard: their name, where they stand, and how many of its games they have abandoned.
/// </summary>
public sealed record Leader(string Name, Standing Standing)
{
    public int Abandoned { get; init; }
}
