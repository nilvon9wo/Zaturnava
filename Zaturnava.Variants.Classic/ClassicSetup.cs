using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Variants.Classic;

/// <summary>
/// The pinwheel (rules.md §2): seat 0 holds the bottom-left corner, its back rank along the bottom
/// edge from the corner inward - Boat, Horse, Elephant, Raja - with its Pawns in front; each next
/// seat, clockwise, is the same army turned a quarter turn clockwise about the centre.
/// </summary>
public static class ClassicSetup
{
    public const int Side = 8;
    public const int SeatCount = 4;
    private const int BackRow = 0;
    private const int PawnRow = 1;

    public static readonly ImmutableList<string> SeatNames = ["Black", "Yellow", "Green", "Red"];

    private static readonly Direction SeatZeroForward = new(0, 1);
    private static readonly ImmutableList<PieceKindId> BackRank =
        [ClassicPieces.Boat, ClassicPieces.Horse, ClassicPieces.Elephant, ClassicPieces.Raja];

    public static ImmutableList<Seat> Seats() => [.. Enumerable.Range(0, SeatCount).Select(SeatAt)];

    /// <summary>Opposite seats are allies.</summary>
    public static TeamGraph Teams()
    {
        ImmutableList<SeatId> blackAndGreen = [new(0), new(2)];
        ImmutableList<SeatId> yellowAndRed = [new(1), new(3)];
        return TeamGraph.FromTeams([blackAndGreen, yellowAndRed]);
    }

    /// <summary>Each seat's throne: the square its Raja starts on.</summary>
    public static ImmutableDictionary<SeatId, CellId> Thrones(SquareGridTopology grid) =>
        StartingSquares(grid)
            .Where(start => start.Kind == ClassicPieces.Raja)
            .ToImmutableDictionary(start => start.Owner, start => start.Cell);

    public static Position InitialPosition(SquareGridTopology grid) =>
        StartingSquares(grid)
            .Select((start, index) => start.Placed(new PieceId(index)))
            .Aggregate(Position.Empty, (position, placed) => position.Place(placed.Cell, placed.Piece));

    private static Seat SeatAt(int index) => new(new SeatId(index), Turned(SeatZeroForward, index));

    private static IEnumerable<StartingSquare> StartingSquares(SquareGridTopology grid) =>
        Enumerable.Range(0, SeatCount).SelectMany(seat => Army(grid, seat));

    private static IEnumerable<StartingSquare> Army(SquareGridTopology grid, int seat)
    {
        IEnumerable<StartingSquare> backRank =
            BackRank.Select((kind, column) => Square(grid, seat, kind, column, BackRow));
        IEnumerable<StartingSquare> pawns =
            BackRank.Select((_, column) => Square(grid, seat, ClassicPieces.Pawn, column, PawnRow));
        return backRank.Concat(pawns);
    }

    private static StartingSquare Square(SquareGridTopology grid, int seat, PieceKindId kind, int column, int row)
    {
        (int turnedColumn, int turnedRow) = Enumerable
            .Range(0, seat)
            .Aggregate((column, row), (square, _) => TurnedSquare(square));
        return new StartingSquare(new SeatId(seat), kind, grid.CellAt(turnedColumn, turnedRow));
    }

    private static (int Column, int Row) TurnedSquare((int Column, int Row) square) =>
        (square.Row, Side - 1 - square.Column);

    private static Direction Turned(Direction direction, int quarterTurns) =>
        Enumerable
            .Range(0, quarterTurns)
            .Aggregate(direction, (turned, _) => new Direction(turned.Second, -turned.First));
}
