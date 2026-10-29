using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Running;

/// <summary>
/// Asks deciders for decisions and applies them, for as long as the deciding seat has one: the
/// server uses it for its bot seats, and self-play for whole games of bots. Whenever the game waits
/// on dice, <c>dice</c> rolls them first, whoever plays the seat that rolls.
/// </summary>
public static class DecisionLoop
{
    /// <summary>
    /// Rolls whatever dice the game waits on, then plays on while <paramref name="deciderFor"/> gives the
    /// deciding seat a decider, the game is not over, and fewer than <paramref name="maximumSteps"/>
    /// decisions have been made. Rolling is not deciding, so it is never held back: with no steps
    /// left, the dice a person just asked for are still rolled. It loops rather than recurses, so a long game
    /// cannot run out of stack.
    /// </summary>
    public static async Task<Played> PlayWhile(
        [Required] IRulesApplying rules,
        [Required] Played played,
        [Required] Func<SeatId, IDeciding?> deciderFor,
        [Required] IDiceRolling dice,
        int maximumSteps,
        CancellationToken cancellationToken
    )
    {
        Played current = played;
        int stepsLeft = maximumSteps;
        IDeciding? decider = NextDecider(current.State, deciderFor, dice, stepsLeft);
        while (decider is not null)
        {
            stepsLeft = current.State.AwaitedDice is null ? stepsLeft - 1 : stepsLeft;
            current = await Step(rules, current, decider, cancellationToken);
            decider = NextDecider(current.State, deciderFor, dice, stepsLeft);
        }

        return current;
    }

    /// <summary>A whole game in which every seat is played by the decider at its index.</summary>
    public static Task<Played> PlayAll(
        [Required] IRulesApplying rules,
        [Required] ImmutableList<IDeciding> players,
        [Required] IDiceRolling dice,
        int maximumSteps,
        CancellationToken cancellationToken
    )
    {
        Played start = new(rules.CreateInitialState(), []);
        return PlayWhile(rules, start, seat => players[seat.Value], dice, maximumSteps, cancellationToken);
    }

    /// <summary>The dice roller while the game waits on dice; otherwise whoever plays the seat.</summary>
    private static Func<SeatId, IDeciding?> ChanceFirst(
        GameState state,
        Func<SeatId, IDeciding?> deciderFor,
        IDiceRolling dice
    )
    {
        DiceRoller roller = new(dice);
        return state.AwaitedDice is null
            ? deciderFor
            : _ => roller;
    }

    /// <summary>
    /// Who decides next: the dice roller, if the game waits on dice; else, if steps are left, the seat's decider.
    /// </summary>
    private static IDeciding? NextDecider(
        GameState state,
        Func<SeatId, IDeciding?> deciderFor,
        IDiceRolling dice,
        int stepsLeft
    ) =>
        state.AwaitedDice is not null || stepsLeft > 0
            ? DeciderOf(state, ChanceFirst(state, deciderFor, dice))
            : null;

    private static IDeciding? DeciderOf(GameState state, Func<SeatId, IDeciding?> deciderFor) =>
        state.DecidingSeat is { } seat
            ? deciderFor(seat)
            : null;

    private static async Task<Played> Step(
        IRulesApplying rules,
        Played played,
        IDeciding decider,
        CancellationToken cancellationToken
    )
    {
        SeatId seat = played.State.DecidingSeat!.Value; // only called once a deciding seat has a decider
        DecisionRequest request = new(rules, played.State, seat, rules.LegalActions(played.State));
        GameAction action = await decider.Decide(request, cancellationToken);
        Transition transition = rules.Apply(played.State, action);
        return new Played(transition.State, played.Actions.Add(action.Describe()));
    }
}
