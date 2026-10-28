using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

public sealed class Chaturaji2027SetupTest
{
    private readonly SquareGridTopology _grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);

    [Fact]
    public void Seats_WhenListed_FaceAQuarterTurnClockwiseFromTheSeatBefore()
    {
        // Arrange - nothing to arrange

        // Act
        ImmutableList<Seat> seats = Chaturaji2027Setup.Seats();

        // Assert
        Assert.Equal([new(0, 1), new(1, 0), new(0, -1), new(-1, 0)], seats.Select(seat => seat.Forward));
    }

    [Fact]
    public void Teams_WhenFormed_AllyOppositeSeats()
    {
        // Arrange
        TeamGraph teams = Chaturaji2027Setup.Teams();

        // Act
        bool[] alliances =
        [
            teams.AreAllied(new SeatId(0), new SeatId(2)),
            teams.AreAllied(new SeatId(1), new SeatId(3)),
            teams.AreAllied(new SeatId(0), new SeatId(1)),
        ];

        // Assert
        Assert.Equal([true, true, false], alliances);
    }

    [Fact]
    public void Thrones_WhenFound_AreTheRajasStartingSquares()
    {
        // Arrange - nothing beyond the grid

        // Act
        ImmutableDictionary<SeatId, CellId> thrones = Chaturaji2027Setup.Thrones(this._grid);

        // Assert
        Assert.Equal(
            [this._grid.CellAt(3, 0), this._grid.CellAt(0, 4), this._grid.CellAt(4, 7), this._grid.CellAt(7, 3)],
            Enumerable.Range(0, Chaturaji2027Setup.SeatCount).Select(seat => thrones[new SeatId(seat)])
        );
    }

    [Fact]
    public void InitialPosition_WhenSetUp_PlacesThirtyTwoPiecesOnDistinctSquares()
    {
        // Arrange - nothing beyond the grid

        // Act
        Position position = Chaturaji2027Setup.InitialPosition(this._grid);

        // Assert
        Assert.Equal(32, position.Occupants.Values.Select(piece => piece.Id).Distinct().Count());
    }

    [Fact]
    public void InitialPosition_WhenSetUp_LinesUpSeatZerosBackRankFromTheCorner()
    {
        // Arrange - nothing beyond the grid

        // Act
        Position position = Chaturaji2027Setup.InitialPosition(this._grid);

        // Assert
        Assert.Equal(
            [
                Chaturaji2027Pieces.Boat,
                Chaturaji2027Pieces.Horse,
                Chaturaji2027Pieces.Elephant,
                Chaturaji2027Pieces.Raja,
            ],
            Enumerable.Range(0, 4).Select(column => position.PieceAt(this._grid.CellAt(column, 0))!.Kind)
        );
    }

    [Fact]
    public void InitialPosition_WhenSetUp_PutsEachSeatsBoatInItsOwnCorner()
    {
        // Arrange
        CellId[] corners =
            [this._grid.CellAt(0, 0), this._grid.CellAt(0, 7), this._grid.CellAt(7, 7), this._grid.CellAt(7, 0)];

        // Act
        Position position = Chaturaji2027Setup.InitialPosition(this._grid);

        // Assert
        Assert.Equal(
            Enumerable.Range(0, 4).Select(BoatOf),
            corners.Select(position.PieceAt)
        );
    }

    private static Piece BoatOf(int seat) => new(new PieceId(seat * 8), Chaturaji2027Pieces.Boat, new SeatId(seat));
}
