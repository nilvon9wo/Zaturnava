using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Ransoms as a bot weighs them, by what the rules say each piece is worth, and the points the receiver scores
/// for accepting, which count against the one who offers: the offer it is answering,
/// and the offers it could make once its turn is over, or as it passes (passing, it gets no window to offer
/// after) - one prisoner it holds for one its team lost to
/// that prisoner's owner, or, to an ally, one back for nothing. A bot makes one offer a turn at most, and
/// does not make again one declined, until three rounds have passed, the worth on the board has changed by
/// a tenth, or the seat that declined has made an offer of its own; so that an answer is not asked of its
/// opponents over and over. Of an enemy it asks at most three times the worth of what it gives back, an offer
/// the enemy might take. With an enemy, it minds the table too: it gives a leader back only for as much as it
/// gets, and makes no trade that leaves its team weaker against that
/// enemy's team, counting what each has on the board; so an even trade suits a team that is behind,
/// and nothing given back revives a team that has nothing left.
/// </summary>
internal static class Trading
{
    private const int TurnsToForget = 12;
    private const int ChangeToForget = 10;
    private const int MostAskedForEachGiven = 3;

    /// <summary>
    /// The answer to the offer waiting, if one is: accepted if it gives the bot back, with the points it would
    /// score, no less than it asks, but for <paramref name="leeway"/>, and minds the table; null if none waits.
    /// </summary>
    public static GameAction? AnswerTo(DecisionRequest request, int leeway) =>
        request.State.Offer is { } offer
            ? Answered(request, offer, leeway)
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
    /// The offers the bot may make now, each with what it would gain; none during its turn, unless it
    /// <paramref name="isPassing"/>, nor once it has made one this turn. Only prisoners that may go home now
    /// are traded, and none to a seat that has retired.
    /// </summary>
    public static ImmutableList<Trade> TradesOf(DecisionRequest request, bool isPassing = false)
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
                .. held.SelectMany(prisoner => TradesFor(request, prisoner))
                    .Where(trade => MindsTheTable(request, trade.Offer.Receiver, trade.Returning, trade.Gain))
                    .Where(trade => !IsStillDeclined(request, trade.Offer.Offer))
                    .Where(trade => IsSane(request, trade.Offer)),
            ]
            : [];
    }

    private static GameAction Answered(DecisionRequest request, RansomOffer offer, int leeway)
    {
        int gain = WorthOf(request, offer.Given)
            - WorthOf(request, offer.Asked)
            + request.Rules.RansomPointsOf(request.State, offer);
        return MidasBot.Answer(
            request.Seat,
            gain >= -leeway && MindsTheTable(request, offer.From, [.. offer.Given, .. offer.Asked], gain)
        );
    }

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
        int gain
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
    /// Whether the offer is one the receiver might take: anything to an ally; to an enemy, one asking at most
    /// three times the worth of what it gives back (a Pawn for a Horse, but not for an Elephant or a Raja).
    /// </summary>
    private static bool IsSane(DecisionRequest request, OfferAction offer) =>
        request.State.Teams.AreAllied(offer.Receiver, request.Seat)
        || WorthOf(request, offer.Asked) <= MostAskedForEachGiven * WorthOf(request, offer.Given);

    /// <summary>
    /// Whether the receiver declined <paramref name="offer"/> lately: fewer than three rounds of four seats ago,
    /// with the worth on the board changed by less than a tenth since.
    /// </summary>
    private static bool IsStillDeclined(DecisionRequest request, RansomOffer offer)
    {
        int now = WorthOf(request, request.State.Position);
        return request.State.OffersDeclined.Any(declined =>
            declined.Offer == offer
            && request.State.TurnNumber - declined.TurnNumber < TurnsToForget
            && Math.Abs(now - WorthOf(request, declined.Board)) * ChangeToForget <= WorthOf(request, declined.Board)
        );
    }

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

    private static IEnumerable<Trade> TradesFor(DecisionRequest request, Prisoner given)
    {
        GameState state = request.State;
        SeatId owner = given.Piece.Owner;
        int worth = request.Rules.WorthOf(given.Piece.Kind);
        IEnumerable<Trade> exchanges = state.Prisoners
            .Where(asked => asked.Holder == owner
                && state.Teams.AreAllied(asked.Piece.Owner, request.Seat)
                && request.Rules.MayGoBack(state, asked)
            )
            .Select(asked => new OfferAction(request.Seat, owner, [given.Piece.Id], [asked.Piece.Id]))
            .Select(offer => new Trade(
                offer,
                WorthOf(request, offer.Asked) - worth - request.Rules.RansomPointsOf(state, offer.Offer)
            ));
        Trade gift = new(new OfferAction(request.Seat, owner, [given.Piece.Id], []), -worth);
        return state.Teams.AreAllied(owner, request.Seat)
            ? exchanges.Append(gift)
            : exchanges;
    }

    private static int WorthOf(DecisionRequest request, ImmutableList<PieceId> pieces) =>
        pieces.Sum(id => request.WorthOf(PrisonerOf(request, id)));
}

/// <summary>
/// An offer a bot could make, and the worth it would gain, less the points the receiver would score (less than
/// none, for a gift).
/// </summary>
internal sealed record Trade(OfferAction Offer, int Gain)
{
    public bool IsGift => this.Offer.Asked.IsEmpty;

    /// <summary>Every prisoner the offer would send home, given and asked.</summary>
    public ImmutableList<PieceId> Returning => [.. this.Offer.Given, .. this.Offer.Asked];
}
