using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A game as data to learn from, in the same shape the BotRunner writes self-play games in: its ruleset,
/// each seat (its name, its team, who played it - <c>open</c>, <c>person</c> or the bot's id - and its
/// score), every action in order, and how it ended (null while it goes on). Replaying the actions through
/// the ruleset rebuilds every position; the dice rolled are among them. <see cref="Format"/> changes only
/// if the shape does.
/// </summary>
public sealed record GameHistoryDto(
    int Format,
    string? GameId,
    string RulesetId,
    ImmutableList<HistorySeatDto> Seats,
    ImmutableList<ActionDto> Actions,
    HistoryOutcomeDto? Outcome
);

/// <summary>A seat in a game's history: its number, name and team, who played it, and its score.</summary>
public sealed record HistorySeatDto(int Id, string Name, int Team, string Player, int Score);

/// <summary>
/// How a game ended: the seats that won (none for a draw), the kind of ending as the ruleset's code (see
/// <see cref="OutcomeDto"/>), and why, for people.
/// </summary>
public sealed record HistoryOutcomeDto(ImmutableList<int> Winners, string Ending, string Reason);
