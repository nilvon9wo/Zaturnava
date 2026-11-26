namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A player on a game's leaderboard: their place (from 1), name and Elo rating, rounded; their experience, gained in
/// each game they won, their own points and as much again times their share of their team's; how many games they won,
/// lost and drew; and how many they have abandoned: games not over, with no guest at them, that have waited a day or
/// more for their move; null while the server has not yet counted them, as just after it starts.
/// </summary>
public sealed record StandingDto(
    int Place,
    string Name,
    int Elo,
    int Experience,
    int Won,
    int Lost,
    int Drawn,
    int? Abandoned = 0
)
{
    /// <summary>The points they scored themselves, in all the games of it they finished, won or not.</summary>
    public int Points { get; init; }

    /// <summary>Their Glicko-2 rating, rounded, counted from the same games as their Elo.</summary>
    public int Glicko { get; init; }

    /// <summary>How unsure their Glicko-2 rating is, rounded: 350 for a player with no games counted.</summary>
    public int GlickoDeviation { get; init; }

    /// <summary>
    /// Their TrueSkill rating as TrueSkill ranks: the mean less three deviations, to one place; 0 for a player with no
    /// games counted.
    /// </summary>
    public double TrueSkill { get; init; }

    /// <summary>Their TrueSkill mean, to one place: 25 for a player with no games counted.</summary>
    public double TrueSkillMean { get; init; }

    /// <summary>How unsure their TrueSkill mean is, to one place.</summary>
    public double TrueSkillDeviation { get; init; }

    /// <summary>The player's id, whose profile anyone may see (<see cref="GameRoutes.Profile"/>).</summary>
    public string? PlayerId { get; init; }

    /// <summary>Their winning streak now, of every game.</summary>
    public StreakDto Streak { get; init; } = new(0);

    /// <summary>Their winning streak now, of the games that count toward Elo.</summary>
    public StreakDto RatedStreak { get; init; } = new(0);

    /// <summary>Their winning streak now, of the games played against people alone.</summary>
    public StreakDto PeopleStreak { get; init; } = new(0);
}
