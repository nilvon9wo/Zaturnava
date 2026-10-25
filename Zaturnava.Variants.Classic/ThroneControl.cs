using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Variants.Classic;

/// <summary>
/// Who commands which armies (rules.md §6): a seat moves its own army, the army of every throne its
/// Raja sits on (§6.1a), and, while it has a Raja, the army of any ally left without one (§6.1b).
/// An enemy Raja on a seat's throne shares that seat's turn (§6.3).
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
