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
}
