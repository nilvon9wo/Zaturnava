using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Greedy and short-sighted, like the king whose wish turned even his bread to gold: takes whichever
/// legal action scores the most at once, choosing at random among equals. When nothing scores, it moves
/// or rolls at random, passing only when it can do nothing else. It never looks past its own action, so
/// it walks into every trap. Offered a ransom, it counts pieces: it accepts if it gets back at least as
/// many as it gives up.
/// </summary>
public sealed class MidasBot(Random random) : IBotPlaying
{
    public const string BotId = "midas";

    private readonly Random _random = random;

    public BotProfile Profile { get; } = new(
        BotId,
        "Midas",
        "Greedy and short-sighted: grabs whatever scores the most right now, and otherwise moves at random. "
            + "A plain algorithm: no AI.",
        UsesAi: false
    );

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(AnswerTo(request) ?? this.Pick(Choices(request)));

    /// <summary>The answer to the ransom offered, if one is; null if none is.</summary>
    private static GameAction? AnswerTo(DecisionRequest request) =>
        request.State.Offer is { } offer
            ? Answer(request.Seat, offer.Given.Count >= offer.Asked.Count)
            : null;

    private static GameAction Answer(SeatId seat, bool isAccepted) =>
        isAccepted
            ? new AcceptAction(seat)
            : new DeclineAction(seat);

    /// <summary>The best scoring actions, if any score; otherwise every action that does something.</summary>
    private static ImmutableList<GameAction> Choices(DecisionRequest request)
    {
        ImmutableList<(GameAction Action, int Gain)> gains =
            [.. request.LegalActions.Select(action => (action, Gain(request, action)))];
        int best = gains.Max(gain => gain.Gain);
        return best > 0
            ? [.. gains.Where(gain => gain.Gain == best).Select(gain => gain.Action)]
            : Active(request.LegalActions);
    }

    /// <summary>Anything but passing, if there is anything else.</summary>
    private static ImmutableList<GameAction> Active(ImmutableList<GameAction> actions)
    {
        ImmutableList<GameAction> active = actions.RemoveAll(action => action is PassAction);
        return active.IsEmpty ? actions : active;
    }

    private static int Gain(DecisionRequest request, GameAction action) =>
        ScoreOf(request.Rules.Apply(request.State, action).State, request.Seat)
            - ScoreOf(request.State, request.Seat);

    private static int ScoreOf(GameState state, SeatId seat) => state.Scores.GetValueOrDefault(seat);

    private GameAction Pick(ImmutableList<GameAction> choices) => choices[this._random.Next(choices.Count)];
}
