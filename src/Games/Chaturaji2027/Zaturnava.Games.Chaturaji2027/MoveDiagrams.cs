using System.Collections.Concurrent;
using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Each kind's <see cref="MoveDiagram"/> (Proposed), found by the same move generator a game's moves are: the piece
/// alone on an empty board <see cref="Side"/> cells square, on its centre, facing up; where it may move, there; and
/// where it may capture, an enemy put on each cell in turn. A kind that hops is drawn with a piece of its own two
/// cells out along each line it hops on, to hop over; one a river holds back (<paramref name="riverBound"/>), with a
/// river just ahead of it, and nothing across it (the user's report of 2026-11-12: "Pictures speak 1000 words!").
/// What else the board or the game's rules add (the two-cell first move) is not drawn: the kind's powers and
/// restrictions tell of it.
/// </summary>
internal sealed class MoveDiagrams(
    ImmutableDictionary<PieceKindId, PieceKind> kinds,
    ImmutableHashSet<PieceKindId> riverBound
)
{
    /// <summary>As wide as the farthest leap of the catalogue, four cells, either side of the centre.</summary>
    public const int Side = 9;

    private const int Middle = Side / 2;

    private static readonly SquareGridTopology Grid = new(Side, Side);
    private static readonly CellId Centre = Grid.CellAt(Middle, Middle);
    private static readonly Seat Facing = new(new SeatId(0), new Direction(0, 1));
    private static readonly SeatId Enemy = new(1);
    private static readonly PieceKindId Stand = new("diagram-stand");

    private readonly ImmutableDictionary<PieceKindId, PieceKind> _kinds = kinds;
    private readonly MoveGenerator _moves = new(Grid, kinds);
    private readonly ConcurrentDictionary<PieceKindId, MoveDiagram> _drawn = new();

    public MoveDiagram Of(PieceKindId kind) => this._drawn.GetOrAdd(kind, this.Draw);

    private MoveDiagram Draw(PieceKindId kind)
    {
        ImmutableList<CellId> screens = ScreensFor(this._kinds[kind]);
        Position alone = screens.Aggregate(
            Position.Empty.Place(Centre, new Piece(new PieceId(0), kind, Facing.Id)),
            (position, screen) => position.Place(screen, new Piece(new PieceId(1), Stand, Facing.Id))
        );
        int? riverRow = riverBound.Contains(kind) ? Middle + 1 : null;
        return new MoveDiagram(
            Side,
            [
                .. this.MovesOn(alone)
                    .Where(move => move.Captured is null)
                    .Select(move => move.To)
                    .Where(cell => IsOnNearBank(cell, riverRow))
                    .Select(cell => cell.Value)
                    .Order(),
            ],
            [
                .. Grid.Cells
                    .Where(cell => alone.PieceAt(cell) is null)
                    .Where(cell => IsOnNearBank(cell, riverRow))
                    .Where(cell => this.MayCapture(alone, cell))
                    .Select(cell => cell.Value),
            ],
            [.. screens.Select(screen => screen.Value)]
        )
        {
            RiverRow = riverRow,
        };
    }

    /// <summary>Whether <paramref name="cell"/> is short of the river at <paramref name="riverRow"/>, if any.</summary>
    private static bool IsOnNearBank(CellId cell, int? riverRow) => Grid.RowOf(cell) < (riverRow ?? Side);

    /// <summary>
    /// A piece to hop over two cells out along each direction the kind hops (the user's report of 2026-11-12, of a
    /// Cannon's card showing it hop forward only); none, for a kind that never hops.
    /// </summary>
    private static ImmutableList<CellId> ScreensFor(PieceKind kind) =>
    [
        .. kind.Movement
            .Where(rule => rule.IsHopping)
            .SelectMany(rule => Grid.Resolve(rule.Directions, Facing.Forward))
            .Distinct()
            .Select(direction => Grid.CellAt(Middle + (2 * direction.First), Middle + (2 * direction.Second)))
            .OrderBy(cell => cell.Value),
    ];

    /// <summary>Whether the piece may capture on <paramref name="cell"/>, the only enemy put there.</summary>
    private bool MayCapture(Position alone, CellId cell) =>
        this.MovesOn(alone.Place(cell, new Piece(new PieceId(2), Stand, Enemy)))
            .Any(move => move.Captured is not null);

    private ImmutableList<Move> MovesOn(Position position) =>
        this._moves.MovesFrom(position, Centre, Facing, piece => piece.Owner == Enemy);
}
