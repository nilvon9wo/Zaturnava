using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class GrandBoardTest
{
    private static readonly Direction North = new(0, 1);
    private static readonly Direction East = new(1, 0);
    private static readonly Direction South = new(0, -1);
    private static readonly Direction West = new(-1, 0);

    [Fact]
    public void Kingdoms_WhenChaturajisBoardIsSetAsAPinwheel_LieWhereItsArmiesStart()
    {
        // Arrange
        GrandBoard board = new(8, 8, 4, 2);
        ImmutableList<Kingdom> expected =
        [
            new(new SeatId(0), 0, 0, 4, 2, North, true, true),
            new(new SeatId(1), 0, 4, 2, 4, East, true, false),
            new(new SeatId(2), 4, 6, 4, 2, South, false, false),
            new(new SeatId(3), 6, 0, 2, 4, West, false, true),
        ];

        // Act
        ImmutableList<Kingdom> kingdoms = board.Kingdoms;

        // Assert
        Assert.Equal(expected, kingdoms);
    }

    [Fact]
    public void Kingdoms_WhenSetTopAndBottom_LieAlongTheTopAndBottomEdges()
    {
        // Arrange
        GrandBoard board = new(10, 12, 4, 3, Facing.TopAndBottom);
        ImmutableList<Kingdom> expected =
        [
            new(new SeatId(0), 0, 0, 4, 3, North, true, true),
            new(new SeatId(1), 0, 9, 4, 3, South, true, false),
            new(new SeatId(2), 6, 9, 4, 3, South, false, false),
            new(new SeatId(3), 6, 0, 4, 3, North, false, true),
        ];

        // Act
        ImmutableList<Kingdom> kingdoms = board.Kingdoms;

        // Assert
        Assert.Equal(expected, kingdoms);
    }

    [Theory]
    [InlineData(6, 6, 3, 3, Facing.Pinwheel, true)]
    [InlineData(6, 6, 3, 3, Facing.TopAndBottom, true)]
    [InlineData(26, 26, 12, 12, Facing.Pinwheel, true)]
    [InlineData(27, 26, 12, 12, Facing.Pinwheel, false)]
    [InlineData(26, 27, 12, 12, Facing.Pinwheel, false)]
    [InlineData(8, 8, 2, 3, Facing.Pinwheel, false)]
    [InlineData(8, 8, 3, 2, Facing.Pinwheel, false)]
    [InlineData(10, 8, 9, 3, Facing.Pinwheel, false)]
    [InlineData(8, 10, 9, 3, Facing.Pinwheel, false)]
    [InlineData(8, 8, 5, 4, Facing.Pinwheel, false)]
    [InlineData(8, 8, 5, 3, Facing.Pinwheel, true)]
    [InlineData(8, 8, 5, 3, Facing.TopAndBottom, false)]
    [InlineData(8, 8, 4, 4, Facing.TopAndBottom, true)]
    public void IsPlayable_WhenSet_SaysWhetherEveryKingdomFitsOnTheBoardApart(
        int width,
        int length,
        int kingdomWidth,
        int kingdomDepth,
        Facing facing,
        bool expected
    )
    {
        // Arrange
        GrandBoard board = new(width, length, kingdomWidth, kingdomDepth, facing);

        // Act
        bool isPlayable = board.IsPlayable;

        // Assert
        Assert.Equal(expected, isPlayable);
    }

    [Theory]
    [InlineData(Rivers.None, 0, 7, 7, false)]
    [InlineData(Rivers.NorthSouth, 0, 4, 0, true)]
    [InlineData(Rivers.NorthSouth, 0, 3, 7, false)]
    [InlineData(Rivers.EastWest, 0, 7, 4, true)]
    [InlineData(Rivers.EastWest, 0, 7, 3, false)]
    [InlineData(Rivers.Both, 2, 7, 7, false)]
    [InlineData(Rivers.Both, 2, 3, 7, true)]
    [InlineData(Rivers.Both, 2, 7, 3, true)]
    [InlineData(Rivers.Both, 3, 3, 7, true)]
    public void IsAcrossRiver_WhenSet_SaysWhetherACellIsAcrossARiverFromTheKingdom(
        Rivers rivers,
        int seat,
        int column,
        int row,
        bool expected
    )
    {
        // Arrange
        GrandBoard board = new(8, 8, 3, 3, Rivers: rivers);
        Kingdom kingdom = board.Kingdoms[seat];

        // Act
        bool isAcross = board.IsAcrossRiver(kingdom, column, row);

        // Assert
        Assert.Equal(expected, isAcross);
    }
}
