using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Watchful, like the giant of a hundred eyes: on the board it looks at the reply before it moves. It weighs
/// each action by what it gains, less the worst any enemy could do in return: take a piece left unguarded, or
/// win an exchange, or step a leader onto its empty throne; and it keeps out of the way of its allies' attacks.
/// Still one move deep, and blind to the dice. It weighs a ransom the same way (<see cref="ByExposure"/>): a
/// piece back where it can be taken at once is worth little. It gives its allies' pieces back for nothing,
/// then offers the trade that gains it the most, if it loses nothing by it; and it accepts any offer within a
/// point of even.
/// </summary>
public sealed class ArgusBot(Random random) : IBotPlaying
{
    public const string BotId = "argus";

    /// <summary>How much worse than even an offer may be, for Argus to accept it.</summary>
    public const int Leeway = 1;

    private readonly Random _random = random;

    public BotProfile Profile { get; } = new(
        BotId,
        "Argus",
        "Watchful: before it moves, it looks at what its opponents could take in reply, and keeps its pieces "
            + "guarded, its throne safe and its allies' lines of attack open. Trades prisoners close to even, "
            + "but not for a piece that would come back only to be taken. A plain algorithm: no AI.",
        UsesAi: false
    );

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(
                Ending.AnswerTo(request)
                    ?? Trading.AnswerTo(request, Leeway, ByExposure.Instance)
                    ?? SafeTrade(request, false)
                    ?? Trading.BeforePassing(request, Careful.Choose(request, this._random), SafeTrade)
            );

    /// <summary>
    /// A gift to an ally, if one may be made; else, of the trades that lose nothing, the most even, then the one
    /// that gains the most.
    /// </summary>
    private static OfferAction? SafeTrade(DecisionRequest request, bool isPassing) =>
        Trading.TradesOf(request, isPassing, ByExposure.Instance)
            .Where(trade => trade.IsGift || trade.Gain >= 0)
            .OrderByDescending(trade => trade.IsGift)
            .ThenBy(trade => trade.Imbalance)
            .ThenByDescending(trade => trade.Gain)
            .FirstOrDefault()?.Offer;
}
