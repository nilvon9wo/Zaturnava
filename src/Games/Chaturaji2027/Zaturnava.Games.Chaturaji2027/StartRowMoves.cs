using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The Western Pawn's powers (Grand Legions specification §3), for each piece of <paramref name="kinds"/>. One that
/// started within its army's first <paramref name="rows"/> rows may, from the cell it started on, step two cells
/// straight ahead, over an empty cell onto an empty one; one placed further forward never may. And any of them,
/// wherever it started or stands, may take <i>en passant</i> an enemy piece that, from two king's steps away, moved
/// two cells or more in a line against the pawn's forward, whichever way its own army faces, its first step onto a
/// cell diagonally beside the pawn, where it could have stopped (Stated: so, past a pawn on b4 facing a1, from a2 to
/// a4 or a5, but not from a1 or a3; nor a piece that leapt or hopped over a3): by moving onto that cell, taking the
/// piece where it stopped, while it stays there, by a seat whose own turn has not ended since (Stated 2026-11-06).
/// Where each piece started is where the game was dealt it, if it was; else <paramref name="start"/>.
/// </summary>
internal sealed class StartRowMoves(
    SquareGridTopology grid,
    Position start,
    ImmutableHashSet<PieceKindId> kinds,
    int rows
)
{
    private const int EnPassantDistance = 2;

    private readonly SquareGridTopology _grid = grid;
    private readonly Position _start = start;
    private readonly ImmutableHashSet<PieceKindId> _kinds = kinds;
    private readonly int _rows = rows;
    private readonly ConditionalWeakTable<Position, ImmutableDictionary<PieceId, CellId>> _starts = [];

    /// <summary>
    /// The two-cell first move, and each <i>en passant</i> <paramref name="actor"/> may take, of the piece on
    /// <paramref name="cell"/>.
    /// </summary>
    public IEnumerable<Move> MovesFrom(
        GameState state,
        CellId cell,
        Piece piece,
        SeatId actor,
        Func<Piece, bool> isCapturable
    ) =>
        this._kinds.Contains(piece.Kind)
            ?
            [
                .. this.DoubleStepFrom(state, cell, piece),
                .. this.InPassingFrom(state, cell, piece, actor, isCapturable),
            ]
            : [];

    /// <summary>
    /// Each <i>en passant</i> <paramref name="actor"/> may take with the piece on <paramref name="cell"/>.
    /// </summary>
    public IEnumerable<Move> InPassingFrom(
        GameState state,
        CellId cell,
        Piece piece,
        SeatId actor,
        Func<Piece, bool> isCapturable
    ) =>
        this._kinds.Contains(piece.Kind)
            ? this.InPassing(state, cell, piece, actor, isCapturable)
                .Select(taking => new Move(piece, cell, taking.Cell, taking.Passage.Piece))
            : [];

    /// <summary>What <paramref name="move"/> takes <i>en passant</i>, if anything.</summary>
    public Passage? TakenBy(GameState state, MoveAction move, Piece mover, Func<Piece, bool> isCapturable) =>
        state.Position.PieceAt(move.To) is null && this._kinds.Contains(mover.Kind)
            ? this.InPassing(state, move.From, mover, move.Actor, isCapturable)
                .Where(taking => taking.Cell == move.To)
                .Select(taking => taking.Passage)
                .FirstOrDefault()
            : null;

    /// <summary>
    /// After <paramref name="move"/>: a piece that moved on, or was taken, may no longer be taken <i>en passant</i>;
    /// the mover may, if it went two cells or more in a line, any way, through a cell it could have stopped on
    /// (<paramref name="mayStop"/>), not over one it leapt or hopped. Nothing, where no kind has the powers.
    /// </summary>
    public GameState Passed(GameState after, MoveAction move, Piece mover, Func<CellId, bool> mayStop)
    {
        if (this._kinds.IsEmpty)
        {
            return after;
        }

        ImmutableList<CellId> ahead = this._grid.Ray(move.From, this.Heading(move.From, move.To));
        int reached = ahead.IndexOf(move.To);
        ImmutableList<Passage> passages = reached >= 1 && mayStop(ahead[0])
            ? [new Passage(mover, move.From, [.. ahead.Take(reached)], move.To)]
            : [];
        return after with
        {
            Passages =
            [
                .. after.Passages.Where(passage => after.Position.PieceAt(passage.To)?.Id == passage.Piece.Id),
                .. passages,
            ],
        };
    }

    /// <summary>
    /// Two cells straight ahead, both empty, from the cell the piece started on, if that is within its army's start
    /// rows.
    /// </summary>
    private IEnumerable<Move> DoubleStepFrom(GameState state, CellId cell, Piece piece)
    {
        Direction forward = state.SeatOf(piece.Owner).Forward;
        ImmutableList<CellId> ahead = this._grid.Ray(cell, forward);
        bool mayStep = this.StartsOf(state).TryGetValue(piece.Id, out CellId first)
            && first == cell
            && this.IsWithin(first, Behind(forward), this._rows)
            && ahead.Count >= EnPassantDistance
            && ahead.Take(EnPassantDistance).All(next => state.Position.PieceAt(next) is null);
        return mayStep ? [new Move(piece, cell, ahead[1], null)] : [];
    }

    /// <summary>
    /// Each enemy piece <paramref name="pawn"/>, on <paramref name="cell"/>, may take <i>en passant</i> for
    /// <paramref name="actor"/>, having moved against the pawn's forward, and the empty cell, its first step,
    /// diagonally beside <paramref name="cell"/>, that the pawn would take it from.
    /// </summary>
    private IEnumerable<Taking> InPassing(
        GameState state,
        CellId cell,
        Piece pawn,
        SeatId actor,
        Func<Piece, bool> isCapturable
    )
    {
        Direction against = Behind(state.SeatOf(pawn.Owner).Forward);
        return state.Passages
            .Where(passage => !passage.Closed.Contains(actor)
                && this.Distance(cell, passage.From) == EnPassantDistance
                && this.IsDiagonallyBeside(cell, passage.Passed[0])
                && this._grid.Step(passage.From, against) == passage.Passed[0]
                && state.Position.PieceAt(passage.Passed[0]) is null
                && isCapturable(passage.Piece)
            )
            .Select(passage => new Taking(passage, passage.Passed[0]));
    }

    private bool IsDiagonallyBeside(CellId cell, CellId other) =>
        Math.Abs(this._grid.ColumnOf(cell) - this._grid.ColumnOf(other)) == 1
        && Math.Abs(this._grid.RowOf(cell) - this._grid.RowOf(other)) == 1;

    /// <summary>Whether fewer than <paramref name="remaining"/> rows lie behind <paramref name="cell"/>.</summary>
    private bool IsWithin(CellId cell, Direction back, int remaining) =>
        remaining > 0 && (this._grid.Step(cell, back) is not { } behind || this.IsWithin(behind, back, remaining - 1));

    /// <summary>The way from <paramref name="from"/> towards <paramref name="to"/>, a king's step.</summary>
    private Direction Heading(CellId from, CellId to) =>
        new(
            Math.Sign(this._grid.ColumnOf(to) - this._grid.ColumnOf(from)),
            Math.Sign(this._grid.RowOf(to) - this._grid.RowOf(from))
        );

    private int Distance(CellId from, CellId to) =>
        Math.Max(
            Math.Abs(this._grid.ColumnOf(from) - this._grid.ColumnOf(to)),
            Math.Abs(this._grid.RowOf(from) - this._grid.RowOf(to))
        );

    private ImmutableDictionary<PieceId, CellId> StartsOf(GameState state) =>
        this._starts.GetValue(
            state.Start ?? this._start,
            position => position.Occupants.ToImmutableDictionary(placed => placed.Value.Id, placed => placed.Key)
        );

    private static Direction Behind(Direction forward) => new(-forward.First, -forward.Second);

    /// <summary>A piece to take <i>en passant</i>, from <paramref name="Cell"/>.</summary>
    private sealed record Taking(Passage Passage, CellId Cell);
}
