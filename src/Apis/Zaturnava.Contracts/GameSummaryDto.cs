using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A game as a lobby lists it: enough to choose one to watch or join. <see cref="Players"/> says who
/// plays each seat (<c>open</c>, <c>person</c>, or the bot's id), <see cref="Moves"/> how many
/// actions have been taken, <see cref="SeatNames"/> what the ruleset calls each seat, and
/// <see cref="IsOver"/> whether the game has ended.
/// </summary>
public sealed record GameSummaryDto(
    string Id,
    string RulesetId,
    int Version,
    ImmutableList<int> OpenSeats,
    ImmutableList<string> Players,
    int Moves
)
{
    public const string Open = "open";
    public const string Person = "person";

    public ImmutableList<string> SeatNames { get; init; } = [];

    public bool IsOver { get; init; }
}
