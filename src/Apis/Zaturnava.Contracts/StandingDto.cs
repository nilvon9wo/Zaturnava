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
)
{
    /// <summary>The points they scored themselves, in all the games of it they finished, won or not.</summary>
    public int Points { get; init; }

    /// <summary>
    /// How many of the games they won were against bots alone: every seat not on their team a bot's. Likewise
    /// <see cref="LostAgainstBots"/> and <see cref="DrawnAgainstBots"/>.
    /// </summary>
    public int WonAgainstBots { get; init; }

    public int LostAgainstBots { get; init; }

    public int DrawnAgainstBots { get; init; }

    /// <summary>The player's id, whose profile anyone may see (<see cref="GameRoutes.Profile"/>).</summary>
    public string? PlayerId { get; init; }
}
