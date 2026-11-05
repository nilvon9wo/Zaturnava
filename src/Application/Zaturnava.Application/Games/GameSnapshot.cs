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

    /// <summary>While the game waits for its open seats to be taken, who may fill them with bots, and when.</summary>
    public Vacancy? Vacancy { get; init; }

    /// <summary>
    /// The game <paramref name="record"/> describes, replayed by <paramref name="rules"/>: from the scenario it
    /// started from, if any (<see cref="ScenarioStart"/>), else from a new game.
    /// </summary>
    public static GameSnapshot Of([Required] GameRecord record, [Required] IRulesApplying rules)
    {
        Transition replayed = record.Actions
            .Where(descriptor => !ScenarioStart.IsStart(descriptor))
            .Aggregate(
                new Transition(ScenarioStart.StateOf(record.Actions, rules), []),
                (sofar, descriptor) => Replayed(rules, sofar, descriptor)
            );
        ImmutableList<GameAction> legal = record.IsWaiting ? [] : rules.LegalActions(replayed.State);
        return new GameSnapshot(record, rules, replayed.State, replayed.Events, legal);
    }

    /// <summary>
    /// The game replayed, as <see cref="Of"/> does; none, if its rules no longer replay it: an action no longer legal
    /// or no longer known, or a piece or square its rules no longer have, as when a game's armies change.
    /// </summary>
    public static GameSnapshot? TryOf([Required] GameRecord record, [Required] IRulesApplying rules)
    {
        try
        {
            return Of(record, rules);
        }
        catch (Exception unreplayable) when (unreplayable is IllegalActionException
            or MalformedActionException
            or KeyNotFoundException
            or ArgumentException
            or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Whether <paramref name="token"/> holds the seat that acts, and it may act now: the game is not waiting for
    /// its seats to be taken, and the seat is deciding, or it proposes ending the game, rejoins it, votes on
    /// waiting for another, or takes a chance it has, which a seat may do whoever decides, if the rules let it -
    /// voting to stop waiting only once the vote on that seat is open.
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
            FaceOffAction => this.Rules.ChancesOf(this.State, action.Actor).Contains(action),
            _ => false,
        };

    private static Transition Replayed(IRulesApplying rules, Transition sofar, ActionDescriptor descriptor)
    {
        Transition next = rules.Apply(sofar.State, rules.Parse(descriptor));
        return next with { Events = sofar.Events.AddRange(next.Events) };
    }
}
