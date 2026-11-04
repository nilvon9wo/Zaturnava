namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A player on a game's leaderboard: their place (from 1), name and Elo rating, rounded; their experience, gained in
/// each game they won, their own points plus their team's; and how many games they won, lost and drew.
/// </summary>
public sealed record StandingDto(int Place, string Name, int Elo, int Experience, int Won, int Lost, int Drawn);
