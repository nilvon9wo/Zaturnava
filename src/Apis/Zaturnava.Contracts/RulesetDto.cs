using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A ruleset a game can be created with, how many seats it has, and what it calls each.</summary>
public sealed record RulesetDto(string Id, string Name, int SeatCount)
{
    public ImmutableList<string> SeatNames { get; init; } = [];

    /// <summary>Each seat's team, by number: seats of the same number are allies.</summary>
    public ImmutableList<int> SeatTeams { get; init; } = [];
}
