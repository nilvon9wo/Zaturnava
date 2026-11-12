namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A winning streak: how many games in a row a player has won since they last lost, a draw neither adding to it nor
/// ending it; and the mean Elo rating of the opposing teams they were won against, rounded, none for no streak.
/// </summary>
public sealed record StreakDto(int Wins, int? OpponentElo = null);
