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
    /// left, the dice a person just asked for are still rolled.
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
        bool isChance = played.State.AwaitedDice is not null;
        IDeciding? decider = isChance || maximumSteps > 0
            ? DeciderOf(played.State, ChanceFirst(played.State, deciderFor, dice))
            : null;
        int stepsLeft = isChance ? maximumSteps : maximumSteps - 1;
        return decider is null
            ? played
            : await PlayOn(rules, played, decider, deciderFor, dice, stepsLeft, cancellationToken);
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

    private static IDeciding? DeciderOf(GameState state, Func<SeatId, IDeciding?> deciderFor) =>
        state.DecidingSeat is { } seat
            ? deciderFor(seat)
            : null;

    private static async Task<Played> PlayOn(
        IRulesApplying rules,
        Played played,
        IDeciding decider,
        Func<SeatId, IDeciding?> deciderFor,
        IDiceRolling dice,
        int stepsLeft,
        CancellationToken cancellationToken
    )
    {
        Played next = await Step(rules, played, decider, cancellationToken);
        return await PlayWhile(rules, next, deciderFor, dice, stepsLeft, cancellationToken);
    }

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
