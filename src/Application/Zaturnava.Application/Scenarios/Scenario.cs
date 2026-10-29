using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Application.Scenarios;

/// <summary>
/// A game set up part-way, to try a corner of the rules from: which pieces stand where, whose turn it is, the
/// points scored, the prisoners held, and the dice that turn has rolled, if any - each written as people write
/// them ("d4", "Black Pawn", "Yellow"). What it leaves out is as a new game has it, but for the roll-off,
/// which it skips: the turn starts before its roll, unless it gives dice.
/// </summary>
public sealed record Scenario(string RulesetId, string Name, string Description)
{
    /// <summary>What a host calls it: its file's name, say.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Each piece on the board, by its cell: "d4": "Black Pawn".</summary>
    public ImmutableSortedDictionary<string, string> Pieces { get; init; } =
        ImmutableSortedDictionary<string, string>.Empty;

    /// <summary>The seat whose turn it is, "Yellow"; the first seat's, if none.</summary>
    public string? Turn { get; init; }

    /// <summary>The points each seat has scored, by seat: "Black": 5; none, for a seat not given.</summary>
    public ImmutableSortedDictionary<string, int> Scores { get; init; } = ImmutableSortedDictionary<string, int>.Empty;

    public ImmutableList<ScenarioPrisoner> Prisoners { get; init; } = [];

    /// <summary>The dice the turn has rolled and not used yet; none, if it is still to roll.</summary>
    public ImmutableList<int> Dice { get; init; } = [];
}
