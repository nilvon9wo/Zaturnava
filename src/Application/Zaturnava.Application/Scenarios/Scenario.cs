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

    /// <summary>
    /// Each seat's palace, by seat, as the cell at its centre, "Black": "b2": that cell and those about it
    /// (<see cref="Engine.Rules.GameState.Palaces"/>); none, if it gives none (the user's report of 2026-11-19: the
    /// Mercenaries scenario had no palace to place a mercenary in).
    /// </summary>
    public ImmutableSortedDictionary<string, string> Palaces { get; init; } =
        ImmutableSortedDictionary<string, string>.Empty;

    /// <summary>The dice the turn has rolled and not used yet; none, if it is still to roll.</summary>
    public ImmutableList<int> Dice { get; init; } = [];

    /// <summary>
    /// The edition of the ruleset's pieces and rules it is played by (<see cref="ScenarioStart.Edition"/>): the
    /// first, 0, if it says none, as every scenario written before it could.
    /// </summary>
    public int Edition { get; init; }

    /// <summary>
    /// Whether a piece promoted at the far edge is promoted at the edge to its right too
    /// (<see cref="Engine.Rules.GameState.PromotesAtSide"/>); not, if it says not.
    /// </summary>
    public bool PromotesAtSide { get; init; }
}
