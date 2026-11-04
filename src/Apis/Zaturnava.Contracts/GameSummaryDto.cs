using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A game as a lobby lists it: enough to choose one to watch or join. <see cref="Players"/> says who
/// plays each seat (<c>open</c>; for a person, as <see cref="SeatDto.Player"/> says, their name, <c>guest</c> or
/// <c>deleted player</c>; or the bot's id), <see cref="Moves"/> how many
/// actions have been taken, <see cref="SeatNames"/> what the ruleset calls each seat, and
/// <see cref="IsOver"/> whether the game has ended. <see cref="Created"/> and <see cref="LastChanged"/> say when it
/// was created and when it last changed, a move or a seat taken; none, for a game from before they were kept.
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
    /// <summary>Whoever holds a person's seat without having signed in.</summary>
    public const string Guest = "guest";

    public ImmutableList<string> SeatNames { get; init; } = [];

    public bool IsOver { get; init; }

    public DateTimeOffset? Created { get; init; }

    public DateTimeOffset? LastChanged { get; init; }
}
