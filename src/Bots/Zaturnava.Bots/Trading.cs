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
/// answering, and the offers it could make once its turn is over, or as it passes (passing, it gets no window to
/// offer after) - one prisoner it holds for one its team lost to
/// that prisoner's owner, or, to an ally, one back for nothing. A bot makes one offer a turn at most, and
/// does not make again one declined, nor another asking the same back, until three rounds have passed, the worth
/// on the board has changed by a tenth, or the seat that declined has made an offer of its own; so that an answer
/// is not asked of its opponents over and over. Of an enemy it asks at most half again the worth of what it gives
/// back, an offer the enemy might take. A bot that minds the table weighs, with an enemy, whether its team can
/// afford the trade: it gives a leader back only for as much as it gains, and makes no trade that leaves its team
/// weaker against that enemy's team, counting what each has on the board; so an even trade suits a team that is
/// behind, and nothing given back revives a team that has nothing left.
/// </summary>
internal static class Trading
{
    private const int TurnsToForget = 12;
    private const int ChangeToForget = 10;
    private const int MostAskedPerGiven = 3;
    private const int GivenPerMostAsked = 2;

    /// <summary>
    /// The answer to the offer waiting, if one is: accepted if, as <paramref name="judge"/> weighs it, it gains
    /// the bot no less than nothing, but for <paramref name="leeway"/>, and, if the judge minds the table, the
    /// bot's team can afford it; null if none waits.
    /// </summary>
    public static GameAction? AnswerTo(DecisionRequest request, double leeway, ITradeJudging judge) =>
        request.State.Offer is { } offer
            ? Answered(request, offer, leeway, judge)
            : null;

    /// <summary>
    /// What the bot chose on the board, unless that is passing while it may still offer: then the offer
    /// <paramref name="offering"/> would make, if any.
    /// </summary>
    public static GameAction BeforePassing(
        DecisionRequest request,
        GameAction chosen,
        Func<DecisionRequest, bool, OfferAction?> offering
    ) =>
        chosen is PassAction
            ? offering(request, true) ?? chosen
            : chosen;

    /// <summary>
    /// The offers the bot may make now, each with what it would gain as <paramref name="judge"/> weighs it; none
    /// during its turn, unless it <paramref name="isPassing"/>, nor once it has made one this turn. Only prisoners
    /// that may go home now are traded, and none to a seat that has retired.
    /// </summary>
    public static ImmutableList<Trade> TradesOf(DecisionRequest request, bool isPassing, ITradeJudging judge)
    {
        GameState state = request.State;
        bool mayOffer = (state.IsRansomWindow || isPassing)
            && state.OffersMade.All(offer => offer.From != request.Seat)
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
                    .Where(trade => IsAffordable(request, judge, trade.Offer.Offer, trade.Gain)),
            ]
            : [];
    }

    /// <summary>
    /// The state once <paramref name="offer"/> is accepted: offered first, unless it is the offer waiting. Its
    /// prisoners go home as the rules send them, unless one waits on where its owner would have it.
    /// </summary>
    public static GameState After(DecisionRequest request, RansomOffer offer)
    {
        GameState state = request.State;
        GameState offered = state.Offer == offer
            ? state
            : request.Rules.Apply(state, new OfferAction(offer.From, offer.To, offer.Given, offer.Asked)).State;
        return request.Rules.Apply(offered, new AcceptAction(offer.To)).State;
    }

    public static int WorthOf(DecisionRequest request, ImmutableList<PieceId> pieces) =>
        pieces.Sum(id => request.WorthOf(PrisonerOf(request, id)));

    private static GameAction Answered(DecisionRequest request, RansomOffer offer, double leeway, ITradeJudging judge)
    {
        double gain = judge.GainOf(request, offer);
        return MidasBot.Answer(request.Seat, gain >= -leeway && IsAffordable(request, judge, offer, gain));
    }

    private static bool IsAffordable(DecisionRequest request, ITradeJudging judge, RansomOffer offer, double gain) =>
        !judge.MindsTheTable
        || MindsTheTable(
            request,
            offer.From == request.Seat ? offer.To : offer.From,
            [.. offer.Given, .. offer.Asked],
            gain
        );

    /// <summary>
    /// Whether a trade with <paramref name="other"/>, sending <paramref name="returning"/> home, and gaining the
    /// bot <paramref name="gain"/>, is one the bot's team can afford: always with an ally; with an enemy, only if
    /// no leader of theirs goes home but for a gain of none or more, and the bot's team ends no weaker against
    /// theirs, as the worth on the board of each compares, than it started.
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
        return state.Teams.AreAllied(other, request.Seat)
            || ((gain >= 0 || theirPieces.All(piece => !request.Rules.IsLeader(piece.Kind)))
                && (ours + ourGain) * theirs >= ours * (theirs + theirGain));
    }

    /// <summary>
    /// Whether the offer is one the receiver might take: anything to an ally; to an enemy, one asking at most half
    /// again the worth of what it gives back (a Boat for a Horse, but not a Pawn for an Elephant or a Raja): a
    /// person weighs what is on the board more than points, so does not give much for little.
    /// </summary>
    private static bool IsSane(DecisionRequest request, OfferAction offer) =>
        request.State.Teams.AreAllied(offer.Receiver, request.Seat)
        || WorthOf(request, offer.Asked) * GivenPerMostAsked <= MostAskedPerGiven * WorthOf(request, offer.Given);

    /// <summary>
    /// Whether the receiver declined <paramref name="offer"/>, or one like it, lately: fewer than three rounds of
    /// four seats ago, with the worth on the board changed by less than a tenth since. An offer asking the same
    /// pieces back is like it, whatever it gives; so a declined trade is not offered again for other small change.
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

    /// <summary>The same offer; or, asking something back, one asking the same of the same seat.</summary>
    private static bool IsLike(RansomOffer declined, RansomOffer offer) =>
        declined == offer
        || ((declined.From, declined.To) == (offer.From, offer.To)
            && !offer.Asked.IsEmpty
            && declined.Asked.SequenceEqual(offer.Asked));

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
