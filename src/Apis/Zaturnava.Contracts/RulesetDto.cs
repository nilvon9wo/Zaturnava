using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A ruleset a game can be created with, how many seats it has, and what it calls each; and which game it plays, at
/// which size, as one game may be played at several sizes, each its own ruleset.
/// </summary>
public sealed record RulesetDto(string Id, string Name, int SeatCount)
{
    /// <summary>The game's id, whatever its size: the ruleset's own, unless it is one size of several.</summary>
    public string Game { get; init; } = string.Empty;

    /// <summary>What the game is called, whatever its size.</summary>
    public string GameName { get; init; } = string.Empty;

    /// <summary>The size the game is played at ("small"); none, for a game played at one size only.</summary>
    public string? Size { get; init; }

    /// <summary>
    /// Whether the game is a preview: playable, but not yet ready for the public, so a lobby hides it unless asked.
    /// </summary>
    public bool IsPreview { get; init; }

    /// <summary>
    /// Whether each game is dealt its armies at random as it begins, so that a game may ask for them alike, or for
    /// another game's again.
    /// </summary>
    public bool IsDealt { get; init; }

    /// <summary>
    /// Whether a game's players may draft their own armies (<see cref="CreateGameRequest.IsDrafted"/>).
    /// </summary>
    public bool IsDraftable { get; init; }

    /// <summary>
    /// For a game whose players may draft: what each has to spend, unless the game's creator sets another
    /// (<see cref="CreateGameRequest.DraftBudget"/>); null, for a game whose players may not.
    /// </summary>
    public int? DraftBudget { get; init; }

    public ImmutableList<string> SeatNames { get; init; } = [];

    /// <summary>Each seat's team, by number: seats of the same number are allies.</summary>
    public ImmutableList<int> SeatTeams { get; init; } = [];
}
