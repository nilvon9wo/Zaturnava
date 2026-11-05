using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The leader face-off (Grand Legions specification §6.4), as Janggi's <i>bikjang</i>: a piece of
/// <paramref name="callers"/> (the Janggi General) facing an enemy leader, any of <paramref name="leaders"/>, along
/// an open orthogonal line, nothing between, may call a face-off, whoever decides, for as long as the two face each
/// other: each is taken by the other's side at once. A choice, not automatic. A retired seat's leader faces no one
/// (Proposed): taking it would score nothing.
/// </summary>
internal sealed class FaceOff(
    SquareGridTopology grid,
    ImmutableHashSet<PieceKindId> callers,
    ImmutableHashSet<PieceKindId> leaders
)
{
    private static readonly ImmutableList<Direction> Orthogonal = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

    private readonly SquareGridTopology _grid = grid;
    private readonly ImmutableHashSet<PieceKindId> _callers = callers;
    private readonly ImmutableHashSet<PieceKindId> _leaders = leaders;

    /// <summary>Each face-off <paramref name="seat"/> may call, by a caller of its <paramref name="armies"/>.</summary>
    public IEnumerable<FaceOffAction> ChancesOf(GameState state, SeatId seat, ImmutableHashSet<SeatId> armies) =>
        state.Position.Occupants
            .Where(occupant => armies.Contains(occupant.Value.Owner) && this._callers.Contains(occupant.Value.Kind))
            .SelectMany(occupant => Orthogonal
                .Select(way => this.FacedFrom(state, occupant.Key, occupant.Value, way))
                .OfType<CellId>()
                .Select(faced => new FaceOffAction(seat, occupant.Key, faced))
            );

    /// <summary>The cell of the enemy leader the caller faces that way, if it faces one.</summary>
    private CellId? FacedFrom(GameState state, CellId cell, Piece caller, Direction way)
    {
        CellId? first = this._grid.Ray(cell, way)
            .Select(next => (CellId?)next)
            .FirstOrDefault(next => state.Position.PieceAt(next!.Value) is not null);
        Piece? faced = first is { } at ? state.Position.PieceAt(at) : null;
        return faced is not null
               && this._leaders.Contains(faced.Kind)
               && !state.Teams.AreAllied(faced.Owner, caller.Owner)
               && !state.Retired.Contains(faced.Owner)
            ? first
            : null;
    }
}
