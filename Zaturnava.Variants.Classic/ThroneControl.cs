using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Variants.Classic;

/// <summary>
/// Who commands which armies (rules.md §6): a seat moves its own army, the army of every throne its
/// Raja sits on (§6.1a), and, while it has a Raja, the army of any ally left without one (§6.1b).
/// An ally may take a seat's turn for it, if the seat lets it (§6.2); an enemy Raja on a seat's throne
/// shares that seat's turn (§6.3).
/// </summary>
internal sealed class ThroneControl(ImmutableDictionary<SeatId, CellId> thrones)
{
    private readonly ImmutableDictionary<SeatId, CellId> _thrones = thrones;
    private readonly ImmutableDictionary<CellId, SeatId> _owners =
        thrones.ToImmutableDictionary(throne => throne.Value, throne => throne.Key);

    /// <summary>The armies the deciding seat may move now: only the one it acts for, if it acts for one.</summary>
    public ImmutableHashSet<SeatId> ArmiesOf(GameState state, SeatId seat) =>
        state.ActingFor is { } occupied
            ? [occupied]
            : [seat, .. this.ThronesHeldBy(state, seat), .. RajalessAllies(state, seat)];

    /// <summary>
    /// Whether <paramref name="seat"/> commands an ally's army, whoever's turn it is: its Raja sits on the
    /// ally's throne, or the ally has no Raja while it has one.
    /// </summary>
    public bool CommandsAnAlly(GameState state, SeatId seat) =>
        RajalessAllies(state, seat).Any()
        || this.ThronesHeldBy(state, seat).Any(throne => state.Teams.AreAllied(throne, seat));

    /// <summary>
    /// The allies who may take <paramref name="seat"/>'s turn for it, with its consent (rules.md §6.2): an
    /// ally with a Raja of its own, while that Raja sits on the seat's throne or the seat has none.
    /// </summary>
    public IEnumerable<SeatId> RegentsFor(GameState state, SeatId seat) =>
        state.Seats
            .Select(ally => ally.Id)
            .Where(ally => ally != seat && state.Teams.AreAllied(ally, seat) && HasRaja(state, ally))
            .Where(ally => !HasRaja(state, seat) || IsRajaOf(state.Position.PieceAt(this._thrones[seat]), ally));

    /// <summary>The seat whose throne <paramref name="cell"/> is; null if it is no one's.</summary>
    public SeatId? OwnerOf(CellId cell) =>
        this._owners.TryGetValue(cell, out SeatId owner)
            ? owner
            : null;

    public bool IsThroneOf(CellId cell, SeatId seat) => this._thrones[seat] == cell;

    /// <summary>The enemy whose Raja sits on <paramref name="seat"/>'s throne; null if none does.</summary>
    public SeatId? OccupierOf(GameState state, SeatId seat)
    {
        Piece? occupant = state.Position.PieceAt(this._thrones[seat]);
        bool isEnemyRaja = occupant?.Kind == ClassicPieces.Raja && !state.Teams.AreAllied(occupant.Owner, seat);
        return isEnemyRaja
            ? occupant!.Owner
            : null;
    }

    private static IEnumerable<SeatId> RajalessAllies(GameState state, SeatId seat) =>
        HasRaja(state, seat)
            ? state.Seats
                .Select(ally => ally.Id)
                .Where(ally => ally != seat && state.Teams.AreAllied(ally, seat) && !HasRaja(state, ally))
            : [];

    private static bool HasRaja(GameState state, SeatId seat) =>
        state.Position.Occupants.Values.Any(piece => piece.Owner == seat && piece.Kind == ClassicPieces.Raja);

    private IEnumerable<SeatId> ThronesHeldBy(GameState state, SeatId seat) =>
        this._thrones
            .Where(throne => throne.Key != seat && IsRajaOf(state.Position.PieceAt(throne.Value), seat))
            .Select(throne => throne.Key);

    private static bool IsRajaOf(Piece? piece, SeatId seat) =>
        piece?.Kind == ClassicPieces.Raja && piece.Owner == seat;
}
