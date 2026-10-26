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
/// that prisoner's owner, or, to an ally, one back for nothing. A bot makes one offer a turn at most,
/// so that an answer is not asked of its opponents over and over. With an enemy, it minds the table
/// too: it never gives a leader back, nor makes a trade that leaves its team weaker against that
/// enemy's team, counting what each has on the board; so an even trade suits a team that is behind,
/// and nothing given back revives a team that has nothing left.
/// </summary>
internal static class Trading
{
    /// <summary>
    /// The answer to the offer waiting, if one is: accepted if it gives the bot back no less than it
    /// asks, but for <paramref name="leeway"/>, and minds the table; null if none waits.
    /// </summary>
    public static GameAction? AnswerTo(DecisionRequest request, int leeway) =>
        request.State.Offer is { } offer
            ? MidasBot.Answer(
                request.Seat,
                WorthOf(request, offer.Given) - WorthOf(request, offer.Asked) >= -leeway
                    && MindsTheTable(request, offer.From, [.. offer.Given, .. offer.Asked])
            )
            : null;

    /// <summary>
    /// The offers the bot may make now, each with what it would gain; none during its turn, nor once it
    /// has made one this turn.
    /// </summary>
    public static ImmutableList<Trade> TradesOf(DecisionRequest request)
    {
        GameState state = request.State;
        bool mayOffer = state.IsRansomWindow
            && state.OffersMade.All(offer => offer.From != request.Seat)
            && request.Rules.MayOffer(state);
        ImmutableList<Prisoner> held =
        [
            .. state.Prisoners.Where(prisoner =>
                prisoner.Holder == request.Seat && prisoner.Piece.Owner != request.Seat
            ),
        ];
        return mayOffer
            ?
            [
                .. held.SelectMany(prisoner => TradesFor(request, prisoner))
                    .Where(trade => MindsTheTable(request, trade.Offer.Receiver, trade.Returning)),
            ]
            : [];
    }

    /// <summary>
    /// Whether a trade with <paramref name="other"/>, sending <paramref name="returning"/> home, is one
    /// the bot's team can afford: always with an ally; with an enemy, only if no leader of theirs goes
    /// home, and the bot's team ends no weaker against theirs, as the worth on the board of each
    /// compares, than it started.
    /// </summary>
    private static bool MindsTheTable(DecisionRequest request, SeatId other, ImmutableList<PieceId> returning)
    {
        GameState state = request.State;
        ImmutableList<Piece> pieces = [.. returning.Select(id => PrisonerOf(request, id))];
        int ours = OnTheBoard(request, request.Seat);
        int theirs = OnTheBoard(request, other);
        int ourGain = pieces.Where(piece => state.Teams.AreAllied(piece.Owner, request.Seat)).Sum(request.WorthOf);
        ImmutableList<Piece> theirPieces = [.. pieces.Where(piece => state.Teams.AreAllied(piece.Owner, other))];
        int theirGain = theirPieces.Sum(request.WorthOf);
        return state.Teams.AreAllied(other, request.Seat)
            || (theirPieces.All(piece => !request.Rules.IsLeader(piece.Kind))
                && (ours + ourGain) * theirs >= ours * (theirs + theirGain));
    }

    /// <summary>What the pieces on the board of <paramref name="seat"/>'s team are worth.</summary>
    private static int OnTheBoard(DecisionRequest request, SeatId seat) =>
        request.State.Position.Occupants.Values
            .Where(piece => request.State.Teams.AreAllied(piece.Owner, seat))
            .Sum(request.WorthOf);

    private static int WorthOf(this DecisionRequest request, Piece piece) => request.Rules.WorthOf(piece.Kind);

    private static Piece PrisonerOf(DecisionRequest request, PieceId id) =>
        request.State.Prisoners.First(held => held.Piece.Id == id).Piece;

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

    private static int WorthOf(DecisionRequest request, ImmutableList<PieceId> pieces) =>
        pieces.Sum(id => request.WorthOf(PrisonerOf(request, id)));
}

/// <summary>An offer a bot could make, and the worth it would gain (less than none, for a gift).</summary>
internal sealed record Trade(OfferAction Offer, int Gain)
{
    public bool IsGift => this.Offer.Asked.IsEmpty;

    /// <summary>Every prisoner the offer would send home, given and asked.</summary>
    public ImmutableList<PieceId> Returning => [.. this.Offer.Given, .. this.Offer.Asked];
}
