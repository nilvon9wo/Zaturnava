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
/// cells ahead of it to hop over. What the board or the game's rules add (a river, the two-cell first move) is not
/// drawn: the kind's powers and restrictions tell of it.
/// </summary>
internal sealed class MoveDiagrams(ImmutableDictionary<PieceKindId, PieceKind> kinds)
{
    /// <summary>As wide as the farthest leap of the catalogue, four cells, either side of the centre.</summary>
    public const int Side = 9;

    private static readonly SquareGridTopology Grid = new(Side, Side);
    private static readonly CellId Centre = Grid.CellAt(Side / 2, Side / 2);
    private static readonly CellId Screen = Grid.CellAt(Side / 2, (Side / 2) + 2);
    private static readonly Seat Facing = new(new SeatId(0), new Direction(0, 1));
    private static readonly SeatId Enemy = new(1);
    private static readonly PieceKindId Stand = new("diagram-stand");

    private readonly ImmutableDictionary<PieceKindId, PieceKind> _kinds = kinds;
    private readonly MoveGenerator _moves = new(Grid, kinds);
    private readonly ConcurrentDictionary<PieceKindId, MoveDiagram> _drawn = new();

    public MoveDiagram Of(PieceKindId kind) => this._drawn.GetOrAdd(kind, this.Draw);

    private MoveDiagram Draw(PieceKindId kind)
    {
        ImmutableList<CellId> screens = this._kinds[kind].Movement.Any(rule => rule.IsHopping) ? [Screen] : [];
        Position alone = screens.Aggregate(
            Position.Empty.Place(Centre, new Piece(new PieceId(0), kind, Facing.Id)),
            (position, screen) => position.Place(screen, new Piece(new PieceId(1), Stand, Facing.Id))
        );
        return new MoveDiagram(
            Side,
            [.. this.MovesOn(alone).Where(move => move.Captured is null).Select(move => move.To.Value).Order()],
            [
                .. Grid.Cells
                    .Where(cell => alone.PieceAt(cell) is null)
                    .Where(cell => this.MayCapture(alone, cell))
                    .Select(cell => cell.Value),
            ],
            [.. screens.Select(screen => screen.Value)]
        );
    }

    /// <summary>Whether the piece may capture on <paramref name="cell"/>, the only enemy put there.</summary>
    private bool MayCapture(Position alone, CellId cell) =>
        this.MovesOn(alone.Place(cell, new Piece(new PieceId(2), Stand, Enemy)))
            .Any(move => move.Captured is not null);

    private ImmutableList<Move> MovesOn(Position position) =>
        this._moves.MovesFrom(position, Centre, Facing, piece => piece.Owner == Enemy);
}
