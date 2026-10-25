using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Greedy and short-sighted, like the king whose wish turned even his bread to gold: takes whichever
/// legal action scores the most at once, choosing at random among equals. When nothing scores, it moves
/// or rolls at random, passing only when it can do nothing else. It never looks past its own action, so
/// it walks into every trap. In ransom it counts worth: it accepts an offer that gives it back at least
/// as much as it asks, and, once its turn is over, offers the trade that gains it the most, if any
/// gains it anything. It never gives anything away.
/// </summary>
public sealed class MidasBot(Random random) : IBotPlaying
{
    public const string BotId = "midas";

    private readonly Random _random = random;

    public BotProfile Profile { get; } = new(
        BotId,
        "Midas",
        "Greedy and short-sighted: grabs whatever scores the most right now, and otherwise moves at random. "
            + "Trades prisoners only at a profit. A plain algorithm: no AI.",
        UsesAi: false
    );

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(
                Trading.AnswerTo(request, 0) ?? BestTrade(request) ?? Greedy.Choose(request, this._random)
            );

    /// <summary>The offer that gains the most, if any gains anything.</summary>
    private static OfferAction? BestTrade(DecisionRequest request) =>
        Trading.TradesOf(request).Where(trade => trade.Gain > 0).MaxBy(trade => trade.Gain)?.Offer;

    internal static GameAction Answer(SeatId seat, bool isAccepted) =>
        isAccepted
            ? new AcceptAction(seat)
            : new DeclineAction(seat);
}
