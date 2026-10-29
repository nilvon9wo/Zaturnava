using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A game as data to learn from, the same whether a server hosted it or bots played it out in self-play:
/// its ruleset, each seat (its name, its team, who played it, and its score), every action in order,
/// and how it ended - null while it goes on, or if it never ended. Replaying the actions through the
/// ruleset rebuilds every position; the dice rolled are among them, so the replay is exact.
/// <see cref="Format"/> changes only if the shape does.
/// </summary>
public sealed record GameHistory(
    string RulesetId,
    ImmutableList<HistorySeat> Seats,
    ImmutableList<ActionDescriptor> Actions,
    HistoryOutcome? Outcome
)
{
    public const int CurrentFormat = 1;

    /// <summary>Who played a seat a person held.</summary>
    public const string Person = "person";

    public int Format { get; init; } = CurrentFormat;

    /// <summary>The hosted game's id; null for one played out in self-play.</summary>
    public string? GameId { get; init; }

    /// <summary>The game a server hosts, as it stands.</summary>
    public static GameHistory Of([Required] GameSnapshot snapshot) =>
        Of(
            snapshot.Rules,
            snapshot.State,
            [.. snapshot.Record.Controllers.Select(controller => controller.Player)],
            snapshot.Record.Actions
        ) with
        {
            GameId = snapshot.Record.Id.Value,
        };

    /// <summary>
    /// The game <paramref name="actions"/> played to <paramref name="state"/>, <paramref name="players"/>
    /// saying who played each seat, by number.
    /// </summary>
    public static GameHistory Of(
        [Required] IRulesApplying rules,
        [Required] GameState state,
        [Required] ImmutableList<string> players,
        [Required] ImmutableList<ActionDescriptor> actions
    ) =>
        new(
            rules.Id,
            [
                .. state.Seats.Select(seat => new HistorySeat(
                    seat.Id.Value,
                    rules.NameOf(seat.Id),
                    state.Teams.TeamOf(seat.Id),
                    players[seat.Id.Value],
                    state.Scores.GetValueOrDefault(seat.Id)
                )),
            ],
            actions,
            state.Outcome is { } outcome
                ? new HistoryOutcome([.. outcome.Winners.Select(seat => seat.Value)], outcome.Ending, outcome.Reason)
                : null
        );
}

/// <summary>A seat in a game's history: its number, name and team, who played it, and its final score.</summary>
public sealed record HistorySeat(int Id, string Name, int Team, string Player, int Score);

/// <summary>
/// How a game ended: the seats that won (none for a draw), the kind of ending as the ruleset's code, and
/// why, for people.
/// </summary>
public sealed record HistoryOutcome(ImmutableList<int> Winners, string Ending, string Reason);
