using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Steps along a palace's lines (Grand Legions specification §5), one from each of its cells to its throne, once the
/// throne is set: a piece of <paramref name="steppers"/> in a palace may step along any line through its cell to the
/// next cell the line passes through, toward the throne or away, even in a direction it could not otherwise take.
/// Touching a cell's corner is not passing through it, so a line from a knight's move off the throne passes two cells
/// on the way. A piece of <paramref name="forwardOnly"/> (the Janggi Soldier, which never goes back) only steps so
/// toward its army's front. It moves onto an empty cell, or takes what it may there.
/// </summary>
internal sealed class PalaceSteps(
    SquareGridTopology grid,
    ImmutableHashSet<PieceKindId> steppers,
    ImmutableHashSet<PieceKindId> forwardOnly
)
{
    /// <summary>
    /// The points of a line looked at for the cells it passes through: never exactly on a cell's edge, as no line
    /// within a palace meets an edge at a sample's distance along it.
    /// </summary>
    private const int Samples = 64;

    private readonly SquareGridTopology _grid = grid;
    private readonly ImmutableHashSet<PieceKindId> _steppers = steppers;
    private readonly ImmutableHashSet<PieceKindId> _forwardOnly = forwardOnly;

    /// <summary>Each step the piece on <paramref name="cell"/> may make along a palace line, as its move.</summary>
    public IEnumerable<Move> MovesFrom(GameState state, CellId cell, Piece piece, Func<Piece, bool> isCapturable) =>
        this.ReachFrom(state, cell, piece)
            .Select(target => new Move(piece, cell, target, state.Position.PieceAt(target)))
            .Where(move => move.Captured is null || isCapturable(move.Captured));

    /// <summary>The cells the piece on <paramref name="cell"/> could take on by a step along a palace line.</summary>
    public IEnumerable<Attack> AttacksFrom(GameState state, CellId cell, Piece piece) =>
        this.ReachFrom(state, cell, piece).Select(target => new Attack(piece, cell, target));

    /// <summary>
    /// Each cell the piece on <paramref name="cell"/> could step to along a palace line, whatever is on it.
    /// </summary>
    private IEnumerable<CellId> ReachFrom(GameState state, CellId cell, Piece piece) =>
        this._steppers.Contains(piece.Kind)
            ? (state.Palaces ?? [])
                .Where(palace => palace.Value.Contains(cell))
                .SelectMany(palace => (state.Thrones ?? []).TryGetValue(palace.Key, out CellId throne)
                    ? this.NeighboursOn(palace.Value, throne, cell)
                    : []
                )
                .Where(target => !this._forwardOnly.Contains(piece.Kind)
                    || this.IsForward(cell, target, state.SeatOf(piece.Owner).Forward)
                )
                .Distinct()
            : [];

    /// <summary>The cells next to <paramref name="cell"/> on any line of the palace, either way.</summary>
    private IEnumerable<CellId> NeighboursOn(ImmutableList<CellId> palace, CellId throne, CellId cell) =>
        palace
            .Where(start => start != throne)
            .Select(start => this.PathOf(start, throne))
            .SelectMany(path =>
            {
                int at = path.IndexOf(cell);
                return new[] { at - 1, at + 1 }
                    .Where(next => at >= 0 && next >= 0 && next < path.Count)
                    .Select(next => path[next]);
            });

    /// <summary>
    /// The cells a line from the centre of <paramref name="from"/> to that of <paramref name="to"/> passes through, in
    /// order.
    /// </summary>
    private ImmutableList<CellId> PathOf(CellId from, CellId to)
    {
        (int fromColumn, int fromRow) = (this._grid.ColumnOf(from), this._grid.RowOf(from));
        (int toColumn, int toRow) = (this._grid.ColumnOf(to), this._grid.RowOf(to));
        IEnumerable<CellId> sampled = Enumerable.Range(0, Samples)
            .Select(sample => (sample + 0.5) / Samples)
            .Select(along => this._grid.CellAt(
                (int)Math.Round(fromColumn + ((toColumn - fromColumn) * along)),
                (int)Math.Round(fromRow + ((toRow - fromRow) * along))
            ));
        return sampled.Aggregate(
            ImmutableList<CellId>.Empty,
            (path, next) => !path.IsEmpty && path[^1] == next ? path : path.Add(next)
        );
    }

    /// <summary>
    /// Whether the step from <paramref name="cell"/> to <paramref name="target"/> goes at all forward.
    /// </summary>
    private bool IsForward(CellId cell, CellId target, Direction forward) =>
        ((this._grid.ColumnOf(target) - this._grid.ColumnOf(cell)) * forward.First)
        + ((this._grid.RowOf(target) - this._grid.RowOf(cell)) * forward.Second) > 0;
}
