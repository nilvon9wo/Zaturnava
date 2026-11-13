using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Capture by custody, as in Tafl (Proposed, 2026-11-13, from the designer's sheet): a piece of
/// <paramref name="custodians"/> (the Tafl King, the Swede) that moves next to a piece it may take, along an orthogonal
/// line, with a piece its mover commands just beyond that one, takes it; every such piece about it at once. Only the
/// custodian's own move takes so: a piece that moves between two others is not taken, nor is one that another of the
/// mover's pieces comes to stand beyond.
/// </summary>
internal sealed class Custody(SquareGridTopology grid, ImmutableHashSet<PieceKindId> custodians)
{
    private static readonly ImmutableList<Direction> Orthogonal = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

    private readonly SquareGridTopology _grid = grid;
    private readonly ImmutableHashSet<PieceKindId> _custodians = custodians;

    /// <summary>
    /// The cells of the pieces <paramref name="mover"/>, just moved to <paramref name="cell"/> of
    /// <paramref name="position"/>, takes by custody: each <paramref name="isTaken"/> allows, next to it, with a piece
    /// <paramref name="isHolding"/> allows just beyond.
    /// </summary>
    public ImmutableList<CellId> TakenBy(
        Position position,
        CellId cell,
        Piece mover,
        Func<Piece, bool> isTaken,
        Func<Piece, bool> isHolding
    ) =>
        this._custodians.Contains(mover.Kind)
            ?
            [
                .. Orthogonal
                    .Select(way =>
                        this._grid.Step(cell, way) is { } next
                        && position.PieceAt(next) is { } taken
                        && isTaken(taken)
                        && this._grid.Step(next, way) is { } beyond
                        && position.PieceAt(beyond) is { } holder
                        && isHolding(holder)
                            ? next
                            : (CellId?)null
                    )
                    .OfType<CellId>(),
            ]
            : [];
}
