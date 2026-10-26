using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
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

    /// <summary>The game <paramref name="record"/> describes, replayed by <paramref name="rules"/>.</summary>
    public static GameSnapshot Of([Required] GameRecord record, [Required] IRulesApplying rules)
    {
        Transition replayed = record.Actions.Aggregate(
            new Transition(rules.CreateInitialState(), []),
            (sofar, descriptor) => Replayed(rules, sofar, descriptor)
        );
        return new GameSnapshot(record, rules, replayed.State, replayed.Events, rules.LegalActions(replayed.State));
    }

    /// <summary>
    /// Whether <paramref name="token"/> holds the seat that acts, and it may act now: it is deciding, or it
    /// proposes ending the game, which a seat may do whoever decides, if the rules let it.
    /// </summary>
    public bool MayAct([Required] GameAction action, [Required] string token) =>
        (this.State.DecidingSeat == action.Actor
            || (action is ProposeEndAction && this.Rules.MayProposeEnd(this.State, action.Actor)))
        && this.Record.Controllers[action.Actor.Value].IsHeldBy(token);

    private static Transition Replayed(IRulesApplying rules, Transition sofar, ActionDescriptor descriptor)
    {
        Transition next = rules.Apply(sofar.State, rules.Parse(descriptor));
        return next with { Events = sofar.Events.AddRange(next.Events) };
    }
}
