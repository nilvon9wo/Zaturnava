using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Ransom (rules.md §8): on its own turn, or once it is over, a seat may offer another seat back
/// prisoners it holds of that seat's, for prisoners that seat holds of the offerer's team - or for
/// nothing. The game waits on the receiver, who accepts or declines; either way the offerer decides
/// again. Accepted, each prisoner goes home to its own starting square if free; else to a free starting
/// square of its kind's (any of its army's Pawns' for a Pawn); else to the free squares nearest its own.
/// When it may go to more than one, its owner chooses, and the game waits on that choice. A seat may also give
/// back prisoners of its own team (§8): its own, at once, or an ally's, at once unless an enemy holds the
/// ally's throne; but only once every seat has had a turn since they were taken. Such a prisoner going home
/// takes an enemy on its starting square, by <paramref name="captured"/>, if it is a Raja, or its army's Raja
/// is on its throne.
/// </summary>
internal sealed class Ransom(
    SquareGridTopology grid,
    Position start,
    Func<Transition, Piece, SeatId, CellId, Transition> captured
)
{
    private readonly SquareGridTopology _grid = grid;
    private readonly Func<Transition, Piece, SeatId, CellId, Transition> _captured = captured;

    private readonly ImmutableDictionary<SeatId, CellId> _thrones =
        start.Occupants
            .Where(occupant => occupant.Value.Kind == Chaturaji2027Pieces.Raja)
            .ToImmutableDictionary(occupant => occupant.Value.Owner, occupant => occupant.Key);

    private readonly ImmutableDictionary<PieceId, CellId> _homes =
        start.Occupants.ToImmutableDictionary(occupant => occupant.Value.Id, occupant => occupant.Key);

    /// <summary>For each piece, the starting squares of its army's pieces of its kind, its own among them.</summary>
    private readonly ImmutableDictionary<PieceId, ImmutableList<CellId>> _kin =
        start.Occupants.ToImmutableDictionary(
            occupant => occupant.Value.Id,
            occupant => start.Occupants
                .Where(other => other.Value.Owner == occupant.Value.Owner && other.Value.Kind == occupant.Value.Kind)
                .Select(other => other.Key)
                .ToImmutableList()
        );

    /// <summary>Whether <paramref name="seat"/> holds a prisoner it could give back now.</summary>
    public static bool HoldsRansom(GameState state, SeatId seat) =>
        state.Prisoners.Any(prisoner => prisoner.Holder == seat && MayGoBack(state, prisoner));

    /// <summary>
    /// A prisoner may go home at any time, but one its own team took only once every seat has had a turn
    /// since.
    /// </summary>
    public static bool MayGoBack(GameState state, Prisoner prisoner) =>
        !state.Teams.AreAllied(prisoner.Holder, prisoner.Piece.Owner)
        || !state.TakenByOwnTeam.TryGetValue(prisoner.Piece.Id, out int taken)
        || state.TurnNumber >= taken + state.Seats.Count;

    /// <summary>
    /// On its own turn, with no dice awaited, no offer waiting and no promotion due, a seat with prisoners
    /// may offer.
    /// </summary>
    public static bool MayOffer(GameState state) =>
        state is
        {
            Proposal: null,
            Offer: null,
            AwaitedDice: null,
            ActingFor: null,
            PromotionDue: null,
            Returning: null,
            RollOff.IsEmpty: true,
            DecidingSeat: { } seat,
        }
        && HoldsRansom(state, seat);

    /// <summary>
    /// The offer may be made: the offerer may offer, to a seat that has not retired, its own self included,
    /// gives back at least one prisoner it holds of the receiver's, each of which may go home now, asks only
    /// for prisoners the receiver holds of the offerer's team, and nothing of its own team, and has not made
    /// the same offer this turn.
    /// </summary>
    public static bool IsValid(GameState state, OfferAction offer)
    {
        ImmutableList<Prisoner> given = Held(state, offer.Given, offer.Actor, owner => owner == offer.Receiver);
        ImmutableList<Prisoner> asked =
            Held(state, offer.Asked, offer.Receiver, owner => state.Teams.AreAllied(owner, offer.Actor));
        return state.DecidingSeat is { } seat
            && offer.Actor == seat
            && MayOffer(state)
            && !state.Retired.Contains(offer.Receiver)
            && !given.IsEmpty
            && given.Count == offer.Given.Count
            && given.All(prisoner => MayGoBack(state, prisoner))
            && asked.Count == offer.Asked.Count
            && (offer.Asked.IsEmpty || !state.Teams.AreAllied(offer.Receiver, offer.Actor))
            && !state.OffersMade.Contains(offer.Offer);
    }

    public static Transition Offered(GameState state, OfferAction offer)
    {
        GameState waiting = state with
        {
            Offer = offer.Offer,
            DecidingSeat = offer.Receiver,
            OffersMade = state.OffersMade.Add(offer.Offer),
        };
        return new Transition(waiting, [new RansomOffered(offer.Offer)]);
    }

    /// <summary>The prisoners go home at once, given back to their own team; then the offerer decides again.</summary>
    public Transition Released(GameState state, OfferAction offer)
    {
        ImmutableList<Prisoner> going =
            [.. offer.Given.Select(id => state.Prisoners.First(prisoner => prisoner.Piece.Id == id))];
        Transition released = new(
            state with { OffersMade = state.OffersMade.Add(offer.Offer) },
            [new PrisonersReleased(offer.Offer)]
        );
        return this.SentHome(released, going, offer.Actor);
    }

    public static Transition Declined(GameState state)
    {
        RansomOffer offer = state.Offer!; // only a receiver answers, while its offer waits
        GameState answered = state with { Offer = null, DecidingSeat = offer.From };
        return new Transition(answered, [new RansomDeclined(offer)]);
    }

    /// <summary>
    /// The receiver scores for each prisoner it sends home, those asked for, more for a Raja, and for none it
    /// is given back; the prisoners given go home first, then
    /// those asked for, each to the nearest free square.
    /// </summary>
    public Transition Accepted(GameState state)
    {
        RansomOffer offer = state.Offer!; // only a receiver answers, while its offer waits
        ImmutableList<Prisoner> going =
        [
            .. offer.Given.Concat(offer.Asked).Select(id => state.Prisoners.First(prisoner => prisoner.Piece.Id == id)),
        ];
        int points = going
            .Where(prisoner => offer.Asked.Contains(prisoner.Piece.Id))
            .Sum(prisoner => prisoner.Piece.Kind == Chaturaji2027Pieces.Raja
                ? Chaturaji2027Rules.RansomRajaBonus
                : Chaturaji2027Rules.RansomAcceptedBonus
            );
        GameState scored = state with
        {
            Offer = null,
            Scores = state.Scores.SetItem(offer.To, state.Scores[offer.To] + points),
        };
        Transition answered = new(
            scored,
            [
                new RansomAccepted(offer),
                .. points > 0 ? [new RansomRewarded(offer.To, offer.Asked.Count, points)] : Array.Empty<GameEvent>(),
            ]
        );
        return this.SentHome(answered, going, offer.From);
    }

    /// <summary>
    /// Where the first prisoner waiting may go, its owner's choice, the one a seat out of time would be
    /// given first.
    /// </summary>
    public ImmutableList<PlaceAction> PlacesOf(GameState state)
    {
        Prisoner first = state.Returning!.Prisoners[0]; // asked only while one is due
        return [.. this.PlacesFor(state, first).Select(cell => new PlaceAction(first.Piece.Owner, cell))];
    }

    /// <summary>
    /// Where a prisoner escaping on doubles may go (rules.md §7): its own starting square, taking the piece
    /// there if that is an enemy of its army's; else wherever a piece going home from a ransom may.
    /// </summary>
    public ImmutableList<CellId> EscapesFor(GameState state, Prisoner prisoner)
    {
        CellId home = this._homes[prisoner.Piece.Id];
        return state.Position.PieceAt(home) is { } there && !state.Teams.AreAllied(there.Owner, prisoner.Piece.Owner)
            ? [home]
            : this.PlacesFor(state, prisoner);
    }

    /// <summary>The first prisoner waiting goes where its owner chose; the rest follow.</summary>
    public Transition Placed(GameState state, PlaceAction place)
    {
        ReturnsDue due = state.Returning!; // a placement is legal only while one is due
        Transition placed = Returned(new Transition(state, []), due.Prisoners[0], place.To);
        return this.SentHome(placed, due.Prisoners.RemoveAt(0), due.Resume);
    }

    private static ImmutableList<Prisoner> Held(
        GameState state,
        ImmutableList<PieceId> pieces,
        SeatId holder,
        Func<SeatId, bool> isOwner
    ) =>
        [
            .. state.Prisoners.Where(prisoner =>
                pieces.Contains(prisoner.Piece.Id) && prisoner.Holder == holder && isOwner(prisoner.Piece.Owner)
            ),
        ];

    /// <summary>
    /// Each prisoner in turn goes to the one free square nearest its home, until one has a choice of
    /// squares, which the game waits on its owner to make; once all are home, <paramref name="resume"/>
    /// decides again.
    /// </summary>
    private Transition SentHome(Transition transition, ImmutableList<Prisoner> going, SeatId resume)
    {
        GameState state = transition.State;
        CellId? strike = going.IsEmpty ? null : this.StrikeOf(state, going[0]);
        ImmutableList<CellId> places = going.IsEmpty || strike is not null ? [] : this.PlacesFor(state, going[0]);
        return (going.IsEmpty, strike, places.Count) switch
        {
            (true, _, _) => transition with { State = state with { Returning = null, DecidingSeat = resume } },
            (_, { } home, _) => this.SentHome(this.Struck(transition, going[0], home), going.RemoveAt(0), resume),
            (_, _, 1) => this.SentHome(Returned(transition, going[0], places[0]), going.RemoveAt(0), resume),
            _ => transition with
            {
                State = state with { Returning = new ReturnsDue(going, resume), DecidingSeat = going[0].Piece.Owner },
            },
        };
    }

    /// <summary>
    /// The starting square of a prisoner its own team gives back, if it takes the enemy there (rules.md §8):
    /// a Raja, going back to its throne, or any piece whose army's Raja is on its throne; else null.
    /// </summary>
    private CellId? StrikeOf(GameState state, Prisoner prisoner)
    {
        SeatId owner = prisoner.Piece.Owner;
        CellId home = this._homes[prisoner.Piece.Id];
        Piece? there = state.Position.PieceAt(home);
        Piece? onThrone = state.Position.PieceAt(this._thrones[owner]);
        bool isStriking = state.Teams.AreAllied(prisoner.Holder, owner)
            && there is not null
            && !state.Teams.AreAllied(there.Owner, owner)
            && (prisoner.Piece.Kind == Chaturaji2027Pieces.Raja
                || (onThrone?.Kind == Chaturaji2027Pieces.Raja && onThrone.Owner == owner));
        return isStriking ? home : null;
    }

    /// <summary>The prisoner goes home, taking the enemy there, for its own army.</summary>
    private Transition Struck(Transition transition, Prisoner prisoner, CellId home)
    {
        Piece taken = transition.State.Position.PieceAt(home)!; // struck only where an enemy stands
        return this._captured(Returned(transition, prisoner, home), taken, prisoner.Piece.Owner, home);
    }

    private static Transition Returned(Transition transition, Prisoner prisoner, CellId cell)
    {
        GameState state = transition.State;
        GameState returned = state with
        {
            Position = state.Position.Place(cell, prisoner.Piece),
            Prisoners = state.Prisoners.Remove(prisoner),
            TakenByOwnTeam = state.TakenByOwnTeam.Remove(prisoner.Piece.Id),
        };
        return new Transition(returned, transition.Events.Add(new PieceReturned(prisoner.Piece, cell)));
    }

    /// <summary>
    /// The prisoner's own starting square, if free; else the free starting squares of its kind's; else the
    /// free squares fewest king's steps from its own. Nearest its own first; then on its own starting row,
    /// then nearest its army's back rank, then nearest its army's left.
    /// </summary>
    private ImmutableList<CellId> PlacesFor(GameState state, Prisoner prisoner)
    {
        CellId home = this._homes[prisoner.Piece.Id];
        Direction forward = state.SeatOf(prisoner.Piece.Owner).Forward;
        ImmutableList<CellId> free = [.. this._grid.Cells.Where(cell => state.Position.PieceAt(cell) is null)];
        ImmutableList<CellId> kin = [.. this._kin[prisoner.Piece.Id].Where(free.Contains)];
        IEnumerable<CellId> places = (kin.Contains(home), kin.IsEmpty) switch
        {
            (true, _) => [home],
            (_, false) => kin,
            _ => free.Where(cell => this.Distance(home, cell) == free.Min(near => this.Distance(home, near))),
        };
        return
        [
            .. places
                .OrderBy(cell => this.Distance(home, cell))
                .ThenBy(cell => this.RankOf(cell, forward) != this.RankOf(home, forward))
                .ThenBy(cell => this.RankOf(cell, forward))
                .ThenBy(cell => this.FileOf(cell, forward)),
        ];
    }

    /// <summary>How far forward the cell is, for an army facing <paramref name="forward"/>.</summary>
    private int RankOf(CellId cell, Direction forward) =>
        (this._grid.ColumnOf(cell) * forward.First) + (this._grid.RowOf(cell) * forward.Second);

    /// <summary>How far to the right the cell is, for an army facing <paramref name="forward"/>.</summary>
    private int FileOf(CellId cell, Direction forward) =>
        (this._grid.ColumnOf(cell) * forward.Second) - (this._grid.RowOf(cell) * forward.First);

    private int Distance(CellId from, CellId to) =>
        Math.Max(
            Math.Abs(this._grid.ColumnOf(from) - this._grid.ColumnOf(to)),
            Math.Abs(this._grid.RowOf(from) - this._grid.RowOf(to))
        );
}
