namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// One of the server's bots on a game's leaderboard of bots: its place (from 1) and name; the points it scored; and how
/// many games it won, lost and drew, all told and against people alone, every seat not on its team played by a person
/// rather than a bot or an AI. A bot at several seats of one game counts each.
/// </summary>
public sealed record BotStandingDto(int Place, string Name, int Points, int Won, int Lost, int Drawn)
{
    public int WonAgainstPeople { get; init; }

    public int LostAgainstPeople { get; init; }

    public int DrawnAgainstPeople { get; init; }

    /// <summary>The bot's id, as the catalog offers it (<see cref="BotDto.Id"/>).</summary>
    public string? BotId { get; init; }

    /// <summary>
    /// The bot's Elo rating at the game, rounded, to rank it beside players: the mean of its ratings at the game's
    /// sizes (<see cref="BotDto.Ratings"/>), or 1500, the rating everyone starts at, if it is rated at none.
    /// </summary>
    public int Elo { get; init; } = 1500;

    /// <summary>
    /// Its Glicko-2 rating, rounded: its Elo, on the same scale, as people's games against it take it.
    /// </summary>
    public int Glicko { get; init; } = 1500;

    /// <summary>How unsure its Glicko-2 rating is taken to be: little, its many games making it sure.</summary>
    public int GlickoDeviation { get; init; }

    /// <summary>Its TrueSkill rating as TrueSkill ranks, the mean less three deviations, from its Elo.</summary>
    public double TrueSkill { get; init; }

    /// <summary>Its TrueSkill mean, from its Elo, to one place.</summary>
    public double TrueSkillMean { get; init; }

    /// <summary>How unsure its TrueSkill mean is taken to be, to one place.</summary>
    public double TrueSkillDeviation { get; init; }

    /// <summary>
    /// Whether the bot plays by an AI, as its profile says (<see cref="BotDto.UsesAi"/>), not by a plain algorithm.
    /// </summary>
    public bool UsesAi { get; init; }
}
