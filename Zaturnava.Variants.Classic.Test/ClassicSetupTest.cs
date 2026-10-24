using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Variants.Classic.Test;

public sealed class ClassicSetupTest
{
    private readonly SquareGridTopology _grid = new(ClassicSetup.Side, ClassicSetup.Side);

    [Fact]
    public void Seats_WhenListed_FaceAQuarterTurnClockwiseFromTheSeatBefore()
    {
        // Arrange - nothing to arrange

        // Act
        ImmutableList<Seat> seats = ClassicSetup.Seats();

        // Assert
        Assert.Equal([new(0, 1), new(1, 0), new(0, -1), new(-1, 0)], seats.Select(seat => seat.Forward));
    }

    [Fact]
    public void Teams_WhenFormed_AllyOppositeSeats()
    {
        // Arrange
        TeamGraph teams = ClassicSetup.Teams();

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
        ImmutableDictionary<SeatId, CellId> thrones = ClassicSetup.Thrones(this._grid);

        // Assert
        Assert.Equal(
            [this._grid.CellAt(3, 0), this._grid.CellAt(0, 4), this._grid.CellAt(4, 7), this._grid.CellAt(7, 3)],
            Enumerable.Range(0, ClassicSetup.SeatCount).Select(seat => thrones[new SeatId(seat)])
        );
    }

    [Fact]
    public void InitialPosition_WhenSetUp_PlacesThirtyTwoPiecesOnDistinctSquares()
    {
        // Arrange - nothing beyond the grid

        // Act
        Position position = ClassicSetup.InitialPosition(this._grid);

        // Assert
        Assert.Equal(32, position.Occupants.Values.Select(piece => piece.Id).Distinct().Count());
    }

    [Fact]
    public void InitialPosition_WhenSetUp_LinesUpSeatZerosBackRankFromTheCorner()
    {
        // Arrange - nothing beyond the grid

        // Act
        Position position = ClassicSetup.InitialPosition(this._grid);

        // Assert
        Assert.Equal(
            [ClassicPieces.Boat, ClassicPieces.Horse, ClassicPieces.Elephant, ClassicPieces.Raja],
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
        Position position = ClassicSetup.InitialPosition(this._grid);

        // Assert
        Assert.Equal(
            Enumerable.Range(0, 4).Select(BoatOf),
            corners.Select(position.PieceAt)
        );
    }

    private static Piece BoatOf(int seat) => new(new PieceId(seat * 8), ClassicPieces.Boat, new SeatId(seat));
}
