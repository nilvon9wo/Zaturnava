using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The pinwheel (rules.md §2), on a square board or, in Grand Legions, any other (<see cref="At"/>): seat 0's army in
/// the bottom-left corner, facing up; each next seat, clockwise, the same army turned a quarter turn clockwise, into
/// the next corner. Opposite seats are allies.
/// </summary>
public static class Pinwheel
{
    public const int SeatCount = 4;

    private static readonly Direction SeatZeroForward = new(0, 1);

    public static ImmutableList<Seat> Seats() => [.. Enumerable.Range(0, SeatCount).Select(SeatAt)];

    /// <summary>Opposite seats are allies.</summary>
    public static TeamGraph Teams()
    {
        ImmutableList<SeatId> blackAndGreen = [new(0), new(2)];
        ImmutableList<SeatId> yellowAndRed = [new(1), new(3)];
        return TeamGraph.FromTeams([blackAndGreen, yellowAndRed]);
    }

    /// <summary>
    /// Every seat's army on a board <paramref name="side"/> cells square, seat by seat, each in the order of
    /// <paramref name="army"/>: seat 0's, as it stands from the bottom-left corner, turned for each next seat.
    /// </summary>
    public static ImmutableList<Placement> Armies(int side, [Required] ImmutableList<ArmyPiece> army) =>
    [
        .. Enumerable.Range(0, SeatCount).SelectMany(seat => army.Select(piece => At(side, side, seat, piece))),
    ];

    /// <summary>
    /// Each seat's own army on a board <paramref name="side"/> cells square, seat by seat: the seat's of
    /// <paramref name="armies"/>, as it would stand for seat 0, turned for the seat.
    /// </summary>
    public static ImmutableList<Placement> EachOwn(
        int side,
        [Required] ImmutableList<ImmutableList<ArmyPiece>> armies
    ) =>
        EachOwn(side, side, armies);

    /// <summary>
    /// Each seat's own army on a board <paramref name="width"/> cells across and <paramref name="height"/> up, seat by
    /// seat: the seat's of <paramref name="armies"/>, as it stands from its own corner, facing its own way, placed
    /// for the seat (<see cref="At"/>).
    /// </summary>
    public static ImmutableList<Placement> EachOwn(
        int width,
        int height,
        [Required] ImmutableList<ImmutableList<ArmyPiece>> armies
    ) =>
    [
        .. armies.SelectMany((army, seat) => army.Select(piece => At(width, height, seat, piece))),
    ];

    /// <summary>
    /// The board as <paramref name="seat"/> sees it, from its own corner, facing its own way: as wide as the board,
    /// for seats 0 and 2, facing up or down it; as high, for seats 1 and 3, facing across it.
    /// </summary>
    public static (int Width, int Height) FrameOf(int width, int height, int seat) =>
        int.IsEvenInteger(seat) ? (width, height) : (height, width);

    /// <summary>
    /// Where <paramref name="piece"/>, standing as <paramref name="seat"/> sees the board (<see cref="FrameOf"/>), is
    /// on a board <paramref name="width"/> cells across and <paramref name="height"/> up: on a square board, seat 0's
    /// square turned a quarter turn clockwise about the centre for each seat after it.
    /// </summary>
    public static Placement At(int width, int height, int seat, [Required] ArmyPiece piece)
    {
        (int column, int row) = (seat % SeatCount) switch
        {
            0 => (piece.Column, piece.Row),
            1 => (piece.Row, height - 1 - piece.Column),
            2 => (width - 1 - piece.Column, height - 1 - piece.Row),
            _ => (width - 1 - piece.Row, piece.Column),
        };
        return new Placement(new SeatId(seat), piece.Kind, column, row);
    }

    /// <summary>The pieces placed, numbered in order from 0.</summary>
    public static Position PositionOf([Required] SquareGridTopology grid, [Required] ImmutableList<Placement> start) =>
        start
            .Select((placed, index) => (Cell: grid.CellAt(placed.Column, placed.Row), Piece: placed.PieceOf(index)))
            .Aggregate(Position.Empty, (position, placed) => position.Place(placed.Cell, placed.Piece));

    private static Seat SeatAt(int index) => new(new SeatId(index), Turned(SeatZeroForward, index));

    private static Direction Turned(Direction direction, int quarterTurns) =>
        Enumerable
            .Range(0, quarterTurns)
            .Aggregate(direction, (turned, _) => new Direction(turned.Second, -turned.First));
}
