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

    public ImmutableList<string> SeatNames { get; init; } = [];

    /// <summary>Each seat's team, by number: seats of the same number are allies.</summary>
    public ImmutableList<int> SeatTeams { get; init; } = [];
}
