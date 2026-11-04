namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>A player on a leaderboard: their name, and where they stand.</summary>
public sealed record Leader(string Name, Standing Standing);
