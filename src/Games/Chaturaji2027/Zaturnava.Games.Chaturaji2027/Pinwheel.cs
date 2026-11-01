using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The pinwheel (rules.md §2) on a square board: seat 0's army in the bottom-left corner, facing up; each next
/// seat, clockwise, the same army turned a quarter turn clockwise about the centre. Opposite seats are allies.
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
        .. Enumerable.Range(0, SeatCount).SelectMany(seat => army.Select(piece => Turned(side, seat, piece))),
    ];

    /// <summary>The pieces placed, numbered in order from 0.</summary>
    public static Position PositionOf([Required] SquareGridTopology grid, [Required] ImmutableList<Placement> start) =>
        start
            .Select((placed, index) => (Cell: grid.CellAt(placed.Column, placed.Row), Piece: placed.PieceOf(index)))
            .Aggregate(Position.Empty, (position, placed) => position.Place(placed.Cell, placed.Piece));

    private static Seat SeatAt(int index) => new(new SeatId(index), Turned(SeatZeroForward, index));

    private static Placement Turned(int side, int seat, ArmyPiece piece)
    {
        (int column, int row) = Enumerable
            .Range(0, seat)
            .Aggregate((piece.Column, piece.Row), (square, _) => (square.Row, side - 1 - square.Column));
        return new Placement(new SeatId(seat), piece.Kind, column, row);
    }

    private static Direction Turned(Direction direction, int quarterTurns) =>
        Enumerable
            .Range(0, quarterTurns)
            .Aggregate(direction, (turned, _) => new Direction(turned.Second, -turned.First));
}
