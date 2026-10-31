using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class KingdomTest
{
    private static readonly Kingdom ThreeByTwoAtTwoOne = At(2, 1, 3, 2);

    [Theory]
    [InlineData(2, 1, true)]
    [InlineData(4, 2, true)]
    [InlineData(1, 1, false)]
    [InlineData(5, 1, false)]
    [InlineData(2, 0, false)]
    [InlineData(2, 3, false)]
    public void Contains_WhenGivenACell_SaysWhetherItIsInTheKingdom(int column, int row, bool expected)
    {
        // Act
        bool contains = ThreeByTwoAtTwoOne.Contains(column, row);

        // Assert
        Assert.Equal(expected, contains);
    }

    [Theory]
    [InlineData(4, 2, 1, 1, true)]
    [InlineData(5, 1, 1, 1, false)]
    [InlineData(0, 1, 2, 1, false)]
    [InlineData(2, 3, 1, 1, false)]
    [InlineData(2, 0, 1, 1, false)]
    public void Overlaps_WhenGivenAnotherKingdom_SaysWhetherTheyShareACell(
        int column,
        int row,
        int width,
        int height,
        bool expected
    )
    {
        // Arrange
        Kingdom other = At(column, row, width, height);

        // Act
        bool overlaps = ThreeByTwoAtTwoOne.Overlaps(other);

        // Assert
        Assert.Equal(expected, overlaps);
    }

    private static Kingdom At(int column, int row, int width, int height) =>
        new(new SeatId(0), column, row, width, height, new Direction(0, 1), true, true);
}
