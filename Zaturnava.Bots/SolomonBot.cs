using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Fair-minded, like the king who judged between two mothers: plays the board as greedily as Midas,
/// though keeping out of its allies' way (<see cref="Teamwork"/>), and deals in prisoners to see armies
/// whole again. Once its turn is over, it gives its allies' pieces
/// back for nothing, then offers even trades - a prisoner for one of its team's of the same worth. It
/// accepts any offer within a point of even.
/// </summary>
public sealed class SolomonBot(Random random) : IBotPlaying
{
    public const string BotId = "solomon";

    /// <summary>How much worse than even an offer may be, for Solomon to accept it.</summary>
    public const int Leeway = 1;

    private readonly Random _random = random;

    public BotProfile Profile { get; } = new(
        BotId,
        "Solomon",
        "Fair-minded: plays the board greedily, keeping out of its allies' way, gives allies' pieces back, "
            + "trades prisoners evenly, and accepts any offer close to even. A plain algorithm: no AI.",
        UsesAi: false
    );

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(AnswerTo(request) ?? FairTrade(request) ?? Teamwork.Choose(request, this._random));

    internal static GameAction? AnswerTo(DecisionRequest request) => Trading.AnswerTo(request, Leeway);

    /// <summary>A gift to an ally, if one may be made; else an even trade, the most valuable first.</summary>
    internal static OfferAction? FairTrade(DecisionRequest request) =>
        Trading.TradesOf(request)
            .Where(trade => trade.IsGift || trade.Gain == 0)
            .OrderByDescending(trade => trade.IsGift)
            .ThenByDescending(trade => request.Rules.WorthOf(PieceOf(request, trade)))
            .FirstOrDefault()?.Offer;

    private static Engine.Pieces.PieceKindId PieceOf(DecisionRequest request, Trade trade) =>
        request.State.Prisoners.First(held => held.Piece.Id == trade.Offer.Given[0]).Piece.Kind;
}
