using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Conversion, as in Othello (Stated, 2026-11-19, the designer's sheet, the Evangelist): a piece of
/// <paramref name="converters"/> that has just moved converts every unbroken line of enemy pieces, in any of the eight
/// directions from where it stands, that runs from next to it up to a piece of its own player's: each of the line's
/// pieces becomes its player's, where it stands. The pieces of a line must all be one enemy's, and none of them a
/// leader (<paramref name="leaders"/>). Only the converter's own move converts (Proposed, as in Othello): a piece that
/// another of its player's pieces comes to bracket is not converted.
/// </summary>
internal sealed class Conversion(
    SquareGridTopology grid,
    ImmutableHashSet<PieceKindId> converters,
    ImmutableHashSet<PieceKindId> leaders
)
{
    private static readonly ImmutableList<Direction> Ways =
    [
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1),
        new(1, 1),
        new(1, -1),
        new(-1, 1),
        new(-1, -1),
    ];

    private readonly SquareGridTopology _grid = grid;
    private readonly ImmutableHashSet<PieceKindId> _converters = converters;
    private readonly ImmutableHashSet<PieceKindId> _leaders = leaders;

    /// <summary>
    /// The cells of the pieces <paramref name="mover"/>, just moved to <paramref name="cell"/> of
    /// <paramref name="position"/>, converts: each of a line of one seat's pieces that <paramref name="isEnemy"/>
    /// allows, none a leader, bracketed between it and another of its player's pieces.
    /// </summary>
    public ImmutableList<CellId> ConvertedBy(Position position, CellId cell, Piece mover, Func<SeatId, bool> isEnemy) =>
        this._converters.Contains(mover.Kind)
            ? [.. Ways.SelectMany(way => this.Bracketed(position, cell, way, mover.Owner, isEnemy, []))]
            : [];

    /// <summary>
    /// The cells of <paramref name="line"/>, the enemy pieces met so far going <paramref name="way"/> from
    /// <paramref name="at"/>, and of those beyond, if one of <paramref name="owner"/>'s pieces ends it; none, if an
    /// empty cell, the board's edge, a leader, or a piece of another seat does.
    /// </summary>
    private ImmutableList<CellId> Bracketed(
        Position position,
        CellId at,
        Direction way,
        SeatId owner,
        Func<SeatId, bool> isEnemy,
        ImmutableList<CellId> line
    ) =>
        this._grid.Step(at, way) is { } next && position.PieceAt(next) is { } piece
            ? piece.Owner == owner
                ? line
                : isEnemy(piece.Owner)
                    && !this._leaders.Contains(piece.Kind)
                    && (line.IsEmpty || position.PieceAt(line[0])!.Owner == piece.Owner)
                        ? this.Bracketed(position, next, way, owner, isEnemy, line.Add(next))
                        : []
            : [];
}
