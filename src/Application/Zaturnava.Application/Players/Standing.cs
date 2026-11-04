namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// Where a signed-in player stands at one game, whatever its size (<see cref="Game"/>, as a ruleset's game id): their
/// Elo rating, from <see cref="FirstElo"/>; their experience, gained in each game they won: their own points plus
/// their team's (all its seats', theirs too); and how many games they won, lost and drew.
/// </summary>
public sealed record Standing(PlayerId Player, string Game)
{
    /// <summary>The rating a player starts at (Proposed), as anyone not rated is taken to be.</summary>
    public const double FirstElo = 1500;

    public double Elo { get; init; } = FirstElo;

    public int Experience { get; init; }

    public int Won { get; init; }

    public int Lost { get; init; }

    public int Drawn { get; init; }
}
