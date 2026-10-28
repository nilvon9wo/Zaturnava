using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

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
    /// The armies <paramref name="seat"/> commands on its own turns, besides its own, and why: each throne its
    /// Raja sits on first, then each ally left without a Raja.
    /// </summary>
    public IEnumerable<(SeatId Army, CommandBasis Basis)> CommandedBy(GameState state, SeatId seat) =>
        [
            .. this.ThronesHeldBy(state, seat)
                .Select(throne => (throne, ThroneBasis(state.Teams.AreAllied(throne, seat)))),
            .. RajalessAllies(state, seat).Select(ally => (ally, CommandBasis.LeaderlessAlly)),
        ];

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
        bool isEnemyRaja = occupant?.Kind == Chaturaji2027Pieces.Raja
            && !state.Teams.AreAllied(occupant.Owner, seat)
            && !state.Retired.Contains(occupant.Owner);
        return isEnemyRaja
            ? occupant!.Owner
            : null;
    }

    /// <summary>
    /// The allies with no Raja, if the seat has one and has not retired; a retired ally is commanded by no one.
    /// </summary>
    private static IEnumerable<SeatId> RajalessAllies(GameState state, SeatId seat) =>
        HasRaja(state, seat) && !state.Retired.Contains(seat)
            ? state.Seats
                .Select(ally => ally.Id)
                .Where(ally => ally != seat && state.Teams.AreAllied(ally, seat) && !HasRaja(state, ally))
                .Where(ally => !state.Retired.Contains(ally))
            : [];

    private static CommandBasis ThroneBasis(bool isAllied) =>
        isAllied ? CommandBasis.AllyThrone : CommandBasis.EnemyThrone;

    private static bool HasRaja(GameState state, SeatId seat) =>
        state.Position.Occupants.Values.Any(piece => piece.Owner == seat && piece.Kind == Chaturaji2027Pieces.Raja);

    /// <summary>
    /// The seats whose thrones the seat's Raja holds; a retired seat's throne, or a retired seat's Raja, commands
    /// nothing.
    /// </summary>
    private IEnumerable<SeatId> ThronesHeldBy(GameState state, SeatId seat) =>
        state.Retired.Contains(seat)
            ? []
            : this._thrones
                .Where(throne => throne.Key != seat && !state.Retired.Contains(throne.Key))
                .Where(throne => IsRajaOf(state.Position.PieceAt(throne.Value), seat))
                .Select(throne => throne.Key);

    private static bool IsRajaOf(Piece? piece, SeatId seat) =>
        piece?.Kind == Chaturaji2027Pieces.Raja && piece.Owner == seat;
}
