using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class BoardFrameTest
{
    [Fact]
    public void Of_WhenThereAreNoCells_IsEmpty()
    {
        // Arrange - nothing beyond the empty board

        // Act
        BoardFrame frame = BoardFrame.Of([]);

        // Assert
        Assert.Equal((BoardFrame.Empty, "0 0 0 0"), (frame, frame.ViewBox));
    }

    [Fact]
    public void Of_WhenGivenASquareBoard_FramesEveryCellWithItsFirstRowAtTheBottom()
    {
        // Arrange
        ImmutableList<CellDto> cells =
        [
            .. Enumerable
                .Range(0, 64)
                .Select(id => new CellDto(id, id % 8, Math.DivRem(id, 8).Quotient, "square", null, $"{id}")),
        ];

        // Act
        BoardFrame frame = BoardFrame.Of(cells);

        // Assert
        Assert.Equal("-0.5 -7.5 8 8", frame.ViewBox);
    }

    [Fact]
    public void ScreenY_WhenGivenALayoutRow_DrawsItUpwards()
    {
        // Arrange - nothing beyond the row

        // Act
        double y = BoardFrame.ScreenY(3);

        // Assert
        Assert.Equal(-3, y);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 0, false)]
    [InlineData(1, 1, true)]
    [InlineData(-1, 0, false)]
    public void IsDark_WhenGivenACell_AlternatesLikeAChessboard(double x, double y, bool expected)
    {
        // Arrange
        CellDto cell = new(0, x, y, "square", null, "a1");

        // Act
        bool isDark = BoardFrame.IsDark(cell);

        // Assert
        Assert.Equal(expected, isDark);
    }
}
