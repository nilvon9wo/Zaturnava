using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Castling (Grand Legions specification §6.4): a piece of <paramref name="castlers"/> (the Western King) may castle
/// with one of its own army's <paramref name="rooks"/> (a Western Rook) in one orthogonal line with it, nothing
/// between them, stepping two cells towards it, the rook going to the cell the castler passed over. Not across check:
/// as there is no check, the castler may not start on, pass over or land on a cell an enemy piece could capture on
/// (<paramref name="attacked"/>). Allowed even after either piece has moved, or castled before. It is the castler's
/// move: a Leader die allows it, or a move made without rolling.
/// </summary>
internal sealed class Castling(
    SquareGridTopology grid,
    ImmutableHashSet<PieceKindId> castlers,
    ImmutableHashSet<PieceKindId> rooks,
    Func<GameState, SeatId, ImmutableHashSet<CellId>> attacked
)
{
    private const int Steps = 2;

    private static readonly ImmutableList<Direction> Orthogonal =
        [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

    private readonly SquareGridTopology _grid = grid;
    private readonly ImmutableHashSet<PieceKindId> _castlers = castlers;
    private readonly ImmutableHashSet<PieceKindId> _rooks = rooks;
    private readonly Func<GameState, SeatId, ImmutableHashSet<CellId>> _attacked = attacked;

    /// <summary>Each castling the piece on <paramref name="cell"/> may make, as its move.</summary>
    public IEnumerable<Move> MovesFrom(GameState state, CellId cell, Piece piece)
    {
        ImmutableList<Castle> castles = this._castlers.Contains(piece.Kind)
            ? [.. Orthogonal.Select(way => this.CastleFrom(state, cell, piece, way)).OfType<Castle>()]
            : [];
        ImmutableHashSet<CellId> attacked = castles.IsEmpty ? [] : this._attacked(state, piece.Owner);
        return castles
            .Where(castle => !castle.Crossed.Any(attacked.Contains))
            .Select(castle => new Move(piece, cell, castle.KingTo, null));
    }

    /// <summary>The rook <paramref name="move"/> castles with, and where it goes, if the move is a castling.</summary>
    public (CellId From, CellId To)? RookOf(GameState state, MoveAction move, Piece mover) =>
        this._castlers.Contains(mover.Kind)
            ? Orthogonal
                .Select(way => this.CastleFrom(state, move.From, mover, way))
                .OfType<Castle>()
                .Where(castle => castle.KingTo == move.To)
                .Select(castle => ((CellId From, CellId To)?)(castle.RookFrom, castle.RookTo))
                .FirstOrDefault()
            : null;

    /// <summary>
    /// The castling that way, if there is one: the first piece along the line its own army's rook, with at least the
    /// two cells the castler would cross empty before it.
    /// </summary>
    private Castle? CastleFrom(GameState state, CellId cell, Piece piece, Direction way)
    {
        ImmutableList<CellId> line = this._grid.Ray(cell, way);
        int first = line.FindIndex(next => state.Position.PieceAt(next) is not null);
        Piece? rook = first >= 0 ? state.Position.PieceAt(line[first]) : null;
        return first > Steps - 1 && rook!.Owner == piece.Owner && this._rooks.Contains(rook.Kind)
            ? new Castle(line[Steps - 1], [cell, .. line.Take(Steps)], line[first], line[0])
            : null;
    }

    /// <summary>
    /// A castling: where the castler lands, the cells it starts on, passes and lands on, and the rook's cell and where
    /// it goes.
    /// </summary>
    private sealed record Castle(CellId KingTo, ImmutableList<CellId> Crossed, CellId RookFrom, CellId RookTo);
}
