using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class BoardTurnTest
{
    /// <summary>
    /// A four-by-four board: seat 0's throne bottom left, seat 1's top left, 2 top right, 3 bottom right.
    /// </summary>
    private static readonly ImmutableList<CellDto> Cells =
    [
        .. Enumerable
            .Range(0, 16)
            .Select(id => new CellDto(id, id % 4, Math.DivRem(id, 4).Quotient, "square", ThroneAt(id))),
    ];

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 3)]
    [InlineData(2, 2)]
    [InlineData(3, 1)]
    public void HomeOf_WhenTheSeatHasAThrone_TurnsItToTheBottomLeft(int seat, int expected)
    {
        // Arrange
        GameViewDto view = View(Cells, []);

        // Act
        BoardTurn turn = BoardTurn.HomeOf(seat, view);

        // Assert
        Assert.Equal(expected, turn.QuarterTurns);
    }

    [Fact]
    public void HomeOf_WhenTheSeatHasNoThroneButPieces_TurnsTheirMiddleToTheBottomLeft()
    {
        // Arrange
        ImmutableList<CellDto> cells = [.. Cells.Select(cell => cell with { Throne = null })];
        ImmutableList<PieceDto> pieces = [new(0, "pawn", "Pawn", 1, 14), new(1, "pawn", "Pawn", 1, 15)];
        GameViewDto view = View(cells, pieces);

        // Act
        BoardTurn turn = BoardTurn.HomeOf(1, view);

        // Assert
        Assert.Equal(2, turn.QuarterTurns);
    }

    [Fact]
    public void HomeOf_WhenTheSeatHasNeitherThroneNorPieces_DoesNotTurn()
    {
        // Arrange
        ImmutableList<CellDto> cells = [.. Cells.Select(cell => cell with { Throne = null })];

        // Act
        BoardTurn turn = BoardTurn.HomeOf(1, View(cells, []));

        // Assert
        Assert.Equal(BoardTurn.None, turn);
    }

    [Fact]
    public void HomeOf_WhenJustWatching_DoesNotTurn()
    {
        // Arrange - nothing beyond the board

        // Act
        BoardTurn turn = BoardTurn.HomeOf(null, View(Cells, []));

        // Assert
        Assert.Equal(BoardTurn.None, turn);
    }

    [Fact]
    public void Clockwise_WhenTurnedRoundFully_IsBackWhereItStarted()
    {
        // Arrange
        BoardTurn turn = new(3);

        // Act
        BoardTurn turned = turn.Clockwise();

        // Assert
        Assert.Equal((BoardTurn.None, 270), (turned, turn.Angle));
    }

    [Fact]
    public void Anticlockwise_WhenNotTurned_TurnsToThreeQuarters()
    {
        // Arrange - no turn

        // Act
        BoardTurn turned = BoardTurn.None.Anticlockwise();

        // Assert
        Assert.Equal(3, turned.QuarterTurns);
    }

    [Fact]
    public void Transform_WhenTurned_TurnsAboutTheBoardsCentre()
    {
        // Arrange
        BoardFrame frame = BoardFrame.Of(Cells);

        // Act
        BoardTurn turn = new(1);
        (string Board, string Label) transforms = (turn.Transform(frame), turn.Upright(0.5, -2));

        // Assert
        Assert.Equal(("rotate(90 1.5 -1.5)", "rotate(-90 0.5 -2)"), transforms);
    }

    private static int? ThroneAt(int id) =>
        id switch
        {
            0 => 0,
            12 => 1,
            15 => 2,
            3 => 3,
            _ => null,
        };

    private static GameViewDto View(ImmutableList<CellDto> cells, ImmutableList<PieceDto> pieces) =>
        new("g", "test", "Test", 1, 1, 0, cells, pieces, [], [], [], []);
}
