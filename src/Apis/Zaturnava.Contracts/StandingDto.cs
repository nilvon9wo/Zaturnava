namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A player on a game's leaderboard: their place (from 1), name and Elo rating, rounded; their experience, gained in
/// each game they won, their own points plus their team's; how many games they won, lost and drew; and how many they
/// have abandoned: games not over, with no guest at them, that have waited a day or more for their move.
/// </summary>
public sealed record StandingDto(
    int Place,
    string Name,
    int Elo,
    int Experience,
    int Won,
    int Lost,
    int Drawn,
    int Abandoned = 0
);
