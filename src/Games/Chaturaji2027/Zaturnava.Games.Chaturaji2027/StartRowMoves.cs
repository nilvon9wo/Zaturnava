using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Start-row powers (Grand Legions specification §3): a piece of <paramref name="kinds"/> that started within its
/// army's first <paramref name="rows"/> rows may, from the cell it started on, step two cells straight ahead, over an
/// empty cell onto an empty one; and may take <i>en passant</i> an enemy piece that, from two king's steps away,
/// moved two cells or more straight ahead over a cell the piece could capture on: by moving onto that cell, taking the
/// piece where it stopped, until that piece's seat's turn comes round again, or it moves on. A piece placed further
/// forward never has them. Where each piece started is where the game was dealt it, if it was; else
/// <paramref name="start"/>.
/// </summary>
internal sealed class StartRowMoves(
    SquareGridTopology grid,
    MoveGenerator moves,
    Position start,
    ImmutableHashSet<PieceKindId> kinds,
    int rows
)
{
    private const int EnPassantDistance = 2;

    private readonly SquareGridTopology _grid = grid;
    private readonly MoveGenerator _moves = moves;
    private readonly Position _start = start;
    private readonly ImmutableHashSet<PieceKindId> _kinds = kinds;
    private readonly int _rows = rows;
    private readonly ConditionalWeakTable<Position, ImmutableDictionary<PieceId, CellId>> _starts = [];

    /// <summary>The two-cell first move, and each <i>en passant</i>, of the piece on <paramref name="cell"/>.</summary>
    public IEnumerable<Move> MovesFrom(GameState state, CellId cell, Piece piece, Func<Piece, bool> isCapturable) =>
        this.HasPowers(state, piece)
            ?
            [
                .. this.DoubleStepFrom(state, cell, piece),
                .. this.InPassing(state, cell, piece, isCapturable)
                    .Select(taking => new Move(piece, cell, taking.Cell, taking.Passage.Piece)),
            ]
            : [];

    /// <summary>What <paramref name="move"/> takes <i>en passant</i>, if anything.</summary>
    public Passage? TakenBy(GameState state, MoveAction move, Piece mover, Func<Piece, bool> isCapturable) =>
        state.Position.PieceAt(move.To) is null && this.HasPowers(state, mover)
            ? this.InPassing(state, move.From, mover, isCapturable)
                .Where(taking => taking.Cell == move.To)
                .Select(taking => taking.Passage)
                .FirstOrDefault()
            : null;

    /// <summary>
    /// After <paramref name="move"/>: a piece that moved on, or was taken, may no longer be taken <i>en passant</i>;
    /// the mover may, if it went two cells or more straight ahead. Nothing, where no kind has the powers.
    /// </summary>
    public GameState Passed(GameState before, GameState after, MoveAction move, Piece mover)
    {
        if (this._kinds.IsEmpty)
        {
            return after;
        }

        ImmutableList<CellId> ahead = this.Ahead(move.From, before.SeatOf(mover.Owner).Forward);
        int reached = ahead.IndexOf(move.To);
        ImmutableList<Passage> passages = reached >= 1
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

    /// <summary>Whether the piece's kind has start-row powers, and it started within its army's start rows.</summary>
    private bool HasPowers(GameState state, Piece piece) =>
        this._kinds.Contains(piece.Kind)
        && this.StartsOf(state).TryGetValue(piece.Id, out CellId first)
        && this.IsWithin(first, Behind(state.SeatOf(piece.Owner).Forward), this._rows);

    /// <summary>Two cells straight ahead, both empty, from the cell the piece started on.</summary>
    private IEnumerable<Move> DoubleStepFrom(GameState state, CellId cell, Piece piece)
    {
        ImmutableList<CellId> ahead = this.Ahead(cell, state.SeatOf(piece.Owner).Forward);
        bool mayStep = this.StartsOf(state)[piece.Id] == cell
            && ahead.Count >= EnPassantDistance
            && ahead.Take(EnPassantDistance).All(next => state.Position.PieceAt(next) is null);
        return mayStep ? [new Move(piece, cell, ahead[1], null)] : [];
    }

    /// <summary>
    /// Each enemy piece the piece on <paramref name="cell"/> may take <i>en passant</i>, and the empty cell it passed
    /// over that the piece would take it from.
    /// </summary>
    private IEnumerable<Taking> InPassing(GameState state, CellId cell, Piece piece, Func<Piece, bool> isCapturable)
    {
        ImmutableHashSet<CellId> reach =
        [
            .. this._moves.AttacksFrom(state.Position, cell, state.SeatOf(piece.Owner)).Select(attack => attack.Target),
        ];
        return state.Passages
            .Where(passage => this.Distance(cell, passage.From) == EnPassantDistance && isCapturable(passage.Piece))
            .SelectMany(passage => passage.Passed
                .Where(passed => reach.Contains(passed) && state.Position.PieceAt(passed) is null)
                .Select(passed => new Taking(passage, passed))
            );
    }

    /// <summary>Whether fewer than <paramref name="remaining"/> rows lie behind <paramref name="cell"/>.</summary>
    private bool IsWithin(CellId cell, Direction back, int remaining) =>
        remaining > 0 && (this._grid.Step(cell, back) is not { } behind || this.IsWithin(behind, back, remaining - 1));

    /// <summary>Every cell straight ahead of <paramref name="from"/>, nearest first.</summary>
    private ImmutableList<CellId> Ahead(CellId from, Direction forward) =>
        this._grid.Step(from, forward) is { } next
            ? [next, .. this.Ahead(next, forward)]
            : [];

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
