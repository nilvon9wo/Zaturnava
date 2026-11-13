using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

public sealed class PalaceSquaresTest
{
    [Theory]
    [InlineData(3, 3, "1 1")]
    [InlineData(4, 3, "1 1,2 1")]
    [InlineData(4, 2, "1 1,2 1")]
    [InlineData(3, 2, "1 1")]
    [InlineData(2, 2, "")]
    [InlineData(4, 1, "")]
    [InlineData(0, 0, "")]
    public void EdgeCentres_WhenEverySquareOutsideIsFree_IsAnyRoomInsideElseAlongTheEdgeMostlyInside(
        int width,
        int depth,
        string expected
    )
    {
        // Arrange
        ImmutableList<ArmySquare> kingdom = Kingdom(width, depth);

        // Act
        ImmutableList<ArmySquare> centres = PalaceSquares.EdgeCentres(kingdom, _ => true);

        // Assert
        Assert.Equal(expected, Text(centres));
    }

    [Fact]
    public void EdgeCentres_WhenASquareOutsideIsNotFree_StandsNoPalaceOnIt()
    {
        // Arrange - a1 to d2, a3 another army's
        ImmutableList<ArmySquare> kingdom = Kingdom(4, 2);

        // Act
        ImmutableList<ArmySquare> centres =
            PalaceSquares.EdgeCentres(kingdom, square => square != new ArmySquare(0, 2));

        // Assert
        Assert.Equal("2 1", Text(centres));
    }

    [Fact]
    public void EdgeCentres_WhenTooLittleOfThePalaceWouldBeInTheKingdom_IsNone()
    {
        // Arrange - a1, b2 and c1: three squares of the nine about b2
        ImmutableList<ArmySquare> kingdom = [new(0, 0), new(1, 1), new(2, 0)];

        // Act
        ImmutableList<ArmySquare> centres = PalaceSquares.EdgeCentres(kingdom, _ => true);

        // Assert
        Assert.Empty(centres);
    }

    private static ImmutableList<ArmySquare> Kingdom(int width, int depth) =>
    [
        .. Enumerable.Range(0, depth).SelectMany(row =>
            Enumerable.Range(0, width).Select(column => new ArmySquare(column, row))
        ),
    ];

    private static string Text(IEnumerable<ArmySquare> squares) =>
        string.Join(",", squares.Select(square => $"{square.Column} {square.Row}"));
}
