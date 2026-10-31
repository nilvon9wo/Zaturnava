using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A game as it stands: its record, its rules, the replayed state, everything that has happened so far
/// (in order), and what may be done next - and, if the deciding seat's decision is timed, how long it
/// has left.
/// </summary>
public sealed record GameSnapshot(
    GameRecord Record,
    IRulesApplying Rules,
    GameState State,
    ImmutableList<GameEvent> Events,
    ImmutableList<GameAction> LegalActions
)
{
    /// <summary>How long the deciding seat has left to decide; null if it may take as long as it likes.</summary>
    public TimeSpan? TimeLeft { get; init; }

    /// <summary>Who may vote to stop waiting for the deciding seat, and how they stand; null if no one may.</summary>
    public AwayVoting? Away { get; init; }

    /// <summary>
    /// The game <paramref name="record"/> describes, replayed by <paramref name="rules"/>: from the scenario it
    /// started from, if any (<see cref="ScenarioStart"/>), else from a new game.
    /// </summary>
    public static GameSnapshot Of([Required] GameRecord record, [Required] IRulesApplying rules)
    {
        Transition replayed = record.Actions
            .Where(descriptor => descriptor.Kind != ScenarioStart.Kind)
            .Aggregate(
                new Transition(ScenarioStart.StateOf(record.Actions, rules), []),
                (sofar, descriptor) => Replayed(rules, sofar, descriptor)
            );
        ImmutableList<GameAction> legal = record.IsWaiting ? [] : rules.LegalActions(replayed.State);
        return new GameSnapshot(record, rules, replayed.State, replayed.Events, legal);
    }

    /// <summary>
    /// Whether <paramref name="token"/> holds the seat that acts, and it may act now: the game is not waiting for
    /// its seats to be taken, and the seat is deciding, or it proposes ending the game, rejoins it, or votes on
    /// waiting for another, which a seat may do whoever decides, if the rules let it - voting to stop waiting only
    /// once the vote on that seat is open.
    /// </summary>
    public bool MayAct([Required] GameAction action, [Required] string token) =>
        !this.Record.IsWaiting
        && (this.State.DecidingSeat == action.Actor || this.MayActOutOfTurn(action))
        && this.Record.Controllers[action.Actor.Value].IsHeldBy(token);

    private bool MayActOutOfTurn(GameAction action) =>
        action switch
        {
            ProposeEndAction => this.Rules.MayProposeEnd(this.State, action.Actor),
            RejoinAction => this.Rules.MayRejoin(this.State, action.Actor),
            VoteAwayAction { Stops: false } vote => this.Rules.MayVoteAway(this.State, vote.Actor, vote.Absent),
            VoteAwayAction vote => this.Away is { IsOpen: true } away && away.Absent == vote.Absent,
            _ => false,
        };

    private static Transition Replayed(IRulesApplying rules, Transition sofar, ActionDescriptor descriptor)
    {
        Transition next = rules.Apply(sofar.State, rules.Parse(descriptor));
        return next with { Events = sofar.Events.AddRange(next.Events) };
    }
}
