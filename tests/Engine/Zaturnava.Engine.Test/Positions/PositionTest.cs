using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Positions;

public sealed class PositionTest
{
    private static readonly Piece Mover = new(new PieceId(1), new PieceKindId("mover"), new SeatId(0));
    private static readonly Piece Bystander = new(new PieceId(2), new PieceKindId("bystander"), new SeatId(1));

    [Fact]
    public void PieceAt_WhenTheCellIsEmpty_ReturnsNull()
    {
        // Arrange - nothing beyond the empty position

        // Act
        Piece? piece = Position.Empty.PieceAt(new CellId(0));

        // Assert
        Assert.Null(piece);
    }

    [Fact]
    public void Place_WhenCalled_PutsThePieceOnTheCell()
    {
        // Arrange - nothing beyond the empty position

        // Act
        Position position = Position.Empty.Place(new CellId(3), Mover);

        // Assert
        Assert.Equal(Mover, position.PieceAt(new CellId(3)));
    }

    [Fact]
    public void Relocate_WhenTheTargetIsOccupied_ReplacesTheOccupantAndVacatesTheOrigin()
    {
        // Arrange
        Position position = Position.Empty
            .Place(new CellId(0), Mover)
            .Place(new CellId(1), Bystander);

        // Act
        Position relocated = position.Relocate(new CellId(0), new CellId(1));

        // Assert
        Assert.Equal([new KeyValuePair<CellId, Piece>(new CellId(1), Mover)], relocated.Occupants);
    }
}
