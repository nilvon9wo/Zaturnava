using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Variants.Classic;

/// <summary>
/// Ransom (rules.md §8): on its own turn, or once it is over, a seat may offer another seat back
/// prisoners it holds of that seat's, for prisoners that seat holds of the offerer's team - or for
/// nothing. The game waits on the receiver, who accepts or declines; either way the offerer decides
/// again. Accepted, each prisoner goes home to the free square nearest its starting square.
/// </summary>
internal sealed class Ransom(SquareGridTopology grid, ImmutableDictionary<PieceId, CellId> homes)
{
    private readonly SquareGridTopology _grid = grid;
    private readonly ImmutableDictionary<PieceId, CellId> _homes = homes;

    /// <summary>Whether <paramref name="seat"/> holds a prisoner it could offer back to its owner.</summary>
    public static bool HoldsRansom(GameState state, SeatId seat) =>
        state.Prisoners.Any(prisoner => prisoner.Holder == seat && prisoner.Piece.Owner != seat);

    /// <summary>
    /// On its own turn, with no dice awaited, no offer waiting and no promotion due, a seat with prisoners
    /// may offer.
    /// </summary>
    public static bool MayOffer(GameState state) =>
        state is
        {
            Offer: null,
            AwaitedDice: null,
            ActingFor: null,
            PromotionDue: null,
            RollOff.IsEmpty: true,
            DecidingSeat: { } seat,
        }
        && HoldsRansom(state, seat);

    /// <summary>
    /// The offer may be made: the offerer may offer, gives back at least one prisoner it holds of the
    /// receiver's, and asks only for prisoners the receiver holds of the offerer's team.
    /// </summary>
    public static bool IsValid(GameState state, OfferAction offer)
    {
        ImmutableList<Prisoner> given = Held(state, offer.Given, offer.Actor, owner => owner == offer.Receiver);
        ImmutableList<Prisoner> asked =
            Held(state, offer.Asked, offer.Receiver, owner => state.Teams.AreAllied(owner, offer.Actor));
        return state.DecidingSeat is { } seat
            && offer.Actor == seat
            && MayOffer(state)
            && offer.Receiver != offer.Actor
            && !given.IsEmpty
            && given.Count == offer.Given.Count
            && asked.Count == offer.Asked.Count;
    }

    public static Transition Offered(GameState state, OfferAction offer)
    {
        GameState waiting = state with { Offer = offer.Offer, DecidingSeat = offer.Receiver };
        return new Transition(waiting, [new RansomOffered(offer.Offer)]);
    }

    public static Transition Declined(GameState state)
    {
        RansomOffer offer = state.Offer!; // only a receiver answers, while its offer waits
        GameState answered = state with { Offer = null, DecidingSeat = offer.From };
        return new Transition(answered, [new RansomDeclined(offer)]);
    }

    /// <summary>The prisoners given go home first, then those asked for, each to the nearest free square.</summary>
    public Transition Accepted(GameState state)
    {
        RansomOffer offer = state.Offer!; // only a receiver answers, while its offer waits
        GameState answered = state with { Offer = null, DecidingSeat = offer.From };
        return offer.Given
            .Concat(offer.Asked)
            .Select(id => state.Prisoners.First(prisoner => prisoner.Piece.Id == id))
            .Aggregate(new Transition(answered, [new RansomAccepted(offer)]), this.Returned);
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

    private Transition Returned(Transition transition, Prisoner prisoner)
    {
        GameState state = transition.State;
        CellId cell = this.NearestFree(state.Position, this._homes[prisoner.Piece.Id]);
        GameState returned = state with
        {
            Position = state.Position.Place(cell, prisoner.Piece),
            Prisoners = state.Prisoners.Remove(prisoner),
        };
        return new Transition(returned, transition.Events.Add(new PieceReturned(prisoner.Piece, cell)));
    }

    /// <summary>
    /// The free square fewest king's steps from <paramref name="home"/>; among equals, the lowest
    /// numbered. (The receiving player is to choose among equals; that choice is not offered yet.)
    /// </summary>
    private CellId NearestFree(Position position, CellId home) =>
        this._grid.Cells
            .Where(cell => position.PieceAt(cell) is null)
            .MinBy(cell => (this.Distance(home, cell), cell.Value));

    private int Distance(CellId from, CellId to) =>
        Math.Max(
            Math.Abs(this._grid.ColumnOf(from) - this._grid.ColumnOf(to)),
            Math.Abs(this._grid.RowOf(from) - this._grid.RowOf(to))
        );
}
