namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// Where one of the server's bots stands at one game, whatever its size (<see cref="Game"/>, as a ruleset's game id),
/// counting each seat it played (Proposed): the points it scored, and how many games it won, lost and drew, all told
/// and of those against people alone (<see cref="WonAgainstPeople"/>).
/// </summary>
public sealed record BotStanding(string BotId, string Game)
{
    public int Points { get; init; }

    public int Won { get; init; }

    public int Lost { get; init; }

    public int Drawn { get; init; }

    /// <summary>
    /// How many of the games it won were against people alone: every seat not on its team played by a person, signed
    /// in or a guest, not their AI.
    /// </summary>
    public int WonAgainstPeople { get; init; }

    public int LostAgainstPeople { get; init; }

    public int DrawnAgainstPeople { get; init; }
}
