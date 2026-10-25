using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Ransoms as a bot weighs them, by what the rules say each piece is worth: the offer it is answering,
/// and the offers it could make once its turn is over - one prisoner it holds for one its team lost to
/// that prisoner's owner, or, to an ally, one back for nothing - leaving out those made already.
/// </summary>
internal static class Trading
{
    /// <summary>The worth of what the offer gives the bot back, less what it asks; null if none waits.</summary>
    public static int? BalanceOf(DecisionRequest request) =>
        request.State.Offer is { } offer
            ? WorthOf(request, offer.Given) - WorthOf(request, offer.Asked)
            : null;

    /// <summary>The offers the bot may make now, each with what it would gain; none during its turn.</summary>
    public static ImmutableList<Trade> TradesOf(DecisionRequest request)
    {
        GameState state = request.State;
        bool mayOffer = state.IsRansomWindow && request.Rules.MayOffer(state);
        ImmutableList<Prisoner> held =
        [
            .. state.Prisoners.Where(prisoner =>
                prisoner.Holder == request.Seat && prisoner.Piece.Owner != request.Seat
            ),
        ];
        return mayOffer
            ? [.. held.SelectMany(prisoner => TradesFor(request, prisoner)).Where(trade => !IsMade(state, trade))]
            : [];
    }

    private static IEnumerable<Trade> TradesFor(DecisionRequest request, Prisoner given)
    {
        GameState state = request.State;
        SeatId owner = given.Piece.Owner;
        int worth = request.Rules.WorthOf(given.Piece.Kind);
        IEnumerable<Trade> exchanges = state.Prisoners
            .Where(asked => asked.Holder == owner && state.Teams.AreAllied(asked.Piece.Owner, request.Seat))
            .Select(asked => new Trade(
                new OfferAction(request.Seat, owner, [given.Piece.Id], [asked.Piece.Id]),
                request.Rules.WorthOf(asked.Piece.Kind) - worth
            ));
        Trade gift = new(new OfferAction(request.Seat, owner, [given.Piece.Id], []), -worth);
        return state.Teams.AreAllied(owner, request.Seat)
            ? exchanges.Append(gift)
            : exchanges;
    }

    private static bool IsMade(GameState state, Trade trade) => state.OffersMade.Contains(trade.Offer.Offer);

    private static int WorthOf(DecisionRequest request, ImmutableList<PieceId> pieces) =>
        pieces.Sum(id => request.Rules.WorthOf(request.State.Prisoners.First(held => held.Piece.Id == id).Piece.Kind));
}

/// <summary>An offer a bot could make, and the worth it would gain (less than none, for a gift).</summary>
internal sealed record Trade(OfferAction Offer, int Gain)
{
    public bool IsGift => this.Offer.Asked.IsEmpty;
}
