using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Ransoms as a bot deals in them, each weighing them its own way (<see cref="ITradeJudging"/>): the offer it is
/// answering, and the offers it could make whenever the rules let it, during its turn as well as once it is over
/// (Stated 2026-11-07: offered before moving, a trade may bring a piece home to where it can take something at once,
/// and gives less away of how the bot means to use it) - one prisoner it holds for one its team lost to that prisoner's
/// owner, or, to an ally, one back for nothing. A bot makes one offer a turn at most, and, once a seat has declined one
/// of its offers, makes that seat none at all until three rounds have passed or the worth on the board has changed by a
/// tenth (Proposed); so that an answer is not asked of its opponents over and over. Of an enemy it asks no more worth
/// than it gives back (Proposed), so that nearly every offer a person sees is one they might reasonably take (Stated
/// 2026-11-12: at least nine in ten), rather than one to decline unread. With an enemy, a bot weighs whether its team
/// can afford the trade: it gives a leader or a heavy piece (a Rook, say) back only for as much as it gains, and makes
/// no trade that leaves its team weaker against that enemy's team, counting what each has on the board; so an even
/// trade suits a team that is behind, and nothing given back revives a team that has nothing left. A team ahead on
/// points gives no leader nor heavy piece back at all, nor more worth than it gets back.
/// </summary>
internal static class Trading
{
    private const int TurnsToForget = 12;
    private const int ChangeToForget = 10;

    /// <summary>
    /// The answer to the offer waiting, if one is: accepted if, as <paramref name="judge"/> weighs it, it gains
    /// the bot no less than nothing, but for <paramref name="leeway"/>, and the bot's team can afford it; null if
    /// none waits.
    /// </summary>
    public static GameAction? AnswerTo(DecisionRequest request, double leeway, ITradeJudging judge) =>
        request.State.Offer is { } offer
            ? Answered(request, offer, leeway, judge)
            : null;

    /// <summary>
    /// The offers the bot may make now, each with what it would gain as <paramref name="judge"/> weighs it; none
    /// once it has made one this turn. Only prisoners that may go home now are traded, and none to a seat that has
    /// retired.
    /// </summary>
    public static ImmutableList<Trade> TradesOf(DecisionRequest request, ITradeJudging judge)
    {
        GameState state = request.State;
        bool mayOffer = state.OffersMade.All(offer => offer.From != request.Seat)
            && request.Rules.MayOffer(state);
        ImmutableList<Prisoner> held =
        [
            .. state.Prisoners.Where(prisoner =>
                prisoner.Holder == request.Seat
                    && prisoner.Piece.Owner != request.Seat
                    && !state.Retired.Contains(prisoner.Piece.Owner)
                    && request.Rules.MayGoBack(state, prisoner)
            ),
        ];
        return mayOffer
            ?
            [
                .. held.SelectMany(prisoner => OffersFor(request, prisoner))
                    .Where(offer => !IsStillDeclined(request, offer.Offer))
                    .Where(offer => IsSane(request, offer))
                    .Select(offer => new Trade(
                        offer,
                        judge.GainOf(request, offer.Offer),
                        Math.Abs(WorthOf(request, offer.Asked) - WorthOf(request, offer.Given))
                    ))
                    .Where(trade => IsAffordable(request, trade.Offer.Offer, trade.Gain)),
            ]
            : [];
    }

    /// <summary>
    /// The state once <paramref name="offer"/> is accepted: offered first, unless it is the offer waiting. Its
    /// prisoners go home as the rules send them, unless one waits on where its owner would have it. An offer the
    /// rules need no answer to (a gift to an ally, say, sent home at once) is done once offered.
    /// </summary>
    public static GameState After(DecisionRequest request, RansomOffer offer)
    {
        GameState state = request.State;
        GameState offered = state.Offer == offer
            ? state
            : request.Rules.Apply(state, new OfferAction(offer.From, offer.To, offer.Given, offer.Asked)).State;
        AcceptAction accept = new(offer.To);
        return request.Rules.LegalActions(offered).Contains(accept)
            ? request.Rules.Apply(offered, accept).State
            : offered;
    }

    public static int WorthOf(DecisionRequest request, ImmutableList<PieceId> pieces) =>
        pieces.Sum(id => request.WorthOf(PrisonerOf(request, id)));

    private static GameAction Answered(DecisionRequest request, RansomOffer offer, double leeway, ITradeJudging judge)
    {
        double gain = judge.GainOf(request, offer);
        return MidasBot.Answer(request.Seat, gain >= -leeway && IsAffordable(request, offer, gain));
    }

    private static bool IsAffordable(DecisionRequest request, RansomOffer offer, double gain) =>
        MindsTheTable(
            request,
            offer.From == request.Seat ? offer.To : offer.From,
            [.. offer.Given, .. offer.Asked],
            gain
        );

    /// <summary>
    /// Whether a trade with <paramref name="other"/>, sending <paramref name="returning"/> home, and gaining the
    /// bot <paramref name="gain"/>, is one the bot's team can afford: always with an ally; with an enemy, only if
    /// no leader nor heavy piece of theirs goes home but for a gain of none or more, and the bot's team ends no
    /// weaker against theirs, as the worth on the board of each compares, than it started. A team ahead on points
    /// needs no bonus, and gives a team behind no way back: it sends no leader nor heavy piece of theirs home, nor
    /// more worth to them than comes back to it.
    /// </summary>
    private static bool MindsTheTable(
        DecisionRequest request,
        SeatId other,
        ImmutableList<PieceId> returning,
        double gain
    )
    {
        GameState state = request.State;
        ImmutableList<Piece> pieces = [.. returning.Select(id => PrisonerOf(request, id))];
        int ours = OnTheBoard(request, request.Seat);
        int theirs = OnTheBoard(request, other);
        int ourGain = pieces.Where(piece => state.Teams.AreAllied(piece.Owner, request.Seat)).Sum(request.WorthOf);
        ImmutableList<Piece> theirPieces = [.. pieces.Where(piece => state.Teams.AreAllied(piece.Owner, other))];
        int theirGain = theirPieces.Sum(request.WorthOf);
        bool returnsDanger = theirPieces.Any(piece =>
            request.Rules.IsLeader(piece.Kind) || request.Rules.IsHeavy(piece.Kind)
        );
        bool leads = PointsOf(state, request.Seat) > PointsOf(state, other);
        return state.Teams.AreAllied(other, request.Seat)
            || ((!returnsDanger || (gain >= 0 && !leads))
                && (!leads || ourGain >= theirGain)
                && (ours + ourGain) * theirs >= ours * (theirs + theirGain));
    }

    /// <summary>The points <paramref name="seat"/>'s team has scored.</summary>
    private static int PointsOf(GameState state, SeatId seat) =>
        state.Scores.Where(score => state.Teams.AreAllied(score.Key, seat)).Sum(score => score.Value);

    /// <summary>
    /// Whether the offer is one the receiver might reasonably take: anything to an ally; to an enemy, one asking no
    /// more worth than it gives back (a Horse for a Horse, or a Pawn for a Boat, but not a Boat for a Pawn): a person
    /// weighs what is on the board more than points, so takes no trade that leaves them worse off on it.
    /// </summary>
    private static bool IsSane(DecisionRequest request, OfferAction offer) =>
        request.State.Teams.AreAllied(offer.Receiver, request.Seat)
        || WorthOf(request, offer.Asked) <= WorthOf(request, offer.Given);

    /// <summary>
    /// Whether the receiver declined <paramref name="offer"/>, or any other of the bot's, lately: fewer than three
    /// rounds of four seats ago, with the worth on the board changed by less than a tenth since. So a seat that
    /// declined is left alone for a while, rather than asked again for other small change.
    /// </summary>
    private static bool IsStillDeclined(DecisionRequest request, RansomOffer offer)
    {
        int now = WorthOf(request, request.State.Position);
        return request.State.OffersDeclined.Any(declined =>
            IsLike(declined.Offer, offer)
            && request.State.TurnNumber - declined.TurnNumber < TurnsToForget
            && Math.Abs(now - WorthOf(request, declined.Board)) * ChangeToForget <= WorthOf(request, declined.Board)
        );
    }

    /// <summary>An offer from the same seat to the same seat.</summary>
    private static bool IsLike(RansomOffer declined, RansomOffer offer) =>
        (declined.From, declined.To) == (offer.From, offer.To);

    private static int WorthOf(DecisionRequest request, Position board) =>
        board.Occupants.Values.Sum(request.WorthOf);

    /// <summary>What the pieces on the board of <paramref name="seat"/>'s team are worth.</summary>
    private static int OnTheBoard(DecisionRequest request, SeatId seat) =>
        request.State.Position.Occupants.Values
            .Where(piece => request.State.Teams.AreAllied(piece.Owner, seat))
            .Sum(request.WorthOf);

    private static int WorthOf(this DecisionRequest request, Piece piece) => request.Rules.WorthOf(piece.Kind);

    private static Piece PrisonerOf(DecisionRequest request, PieceId id) =>
        request.State.Prisoners.First(held => held.Piece.Id == id).Piece;

    /// <summary>
    /// The offers of <paramref name="given"/>: for each prisoner of the bot's team its owner holds; and, to an
    /// ally, for nothing.
    /// </summary>
    private static IEnumerable<OfferAction> OffersFor(DecisionRequest request, Prisoner given)
    {
        GameState state = request.State;
        SeatId owner = given.Piece.Owner;
        IEnumerable<OfferAction> exchanges = state.Prisoners
            .Where(asked => asked.Holder == owner
                && state.Teams.AreAllied(asked.Piece.Owner, request.Seat)
                && request.Rules.MayGoBack(state, asked)
            )
            .Select(asked => new OfferAction(request.Seat, owner, [given.Piece.Id], [asked.Piece.Id]));
        OfferAction gift = new(request.Seat, owner, [given.Piece.Id], []);
        return state.Teams.AreAllied(owner, request.Seat)
            ? exchanges.Append(gift)
            : exchanges;
    }
}

/// <summary>
/// An offer a bot could make, what it would gain, as the bot weighs it, and how far apart the worth of what each
/// side gets back is: none, for an even trade, the kind a person most readily takes.
/// </summary>
internal sealed record Trade(OfferAction Offer, double Gain, int Imbalance)
{
    public bool IsGift => this.Offer.Asked.IsEmpty;
}
