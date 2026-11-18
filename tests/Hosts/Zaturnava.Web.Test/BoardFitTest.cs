using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class BoardFitTest
{
    [Theory]
    [InlineData(10, 10, 4, 3, null)]
    [InlineData(12, 10, 4, 3, null)]
    [InlineData(10, 10, 5, 5, null)]
    [InlineData(26, 26, 23, 3, null)]
    [InlineData(27, 10, 4, 3, "at most 26")]
    [InlineData(10, 27, 4, 3, "at most 26")]
    [InlineData(10, 10, 2, 3, "at least 3")]
    [InlineData(10, 10, 4, 2, "at least 3")]
    [InlineData(12, 6, 7, 3, "at most 6.")]
    [InlineData(6, 12, 3, 7, "at most 6.")]
    [InlineData(10, 10, 6, 5, "together (11)")]
    [InlineData(12, 8, 6, 3, "together (9)")]
    public void ProblemOf_WhenTheKingdomsFitOrNot_SaysWhatIsWrongIfAnything(
        int width,
        int height,
        int kingdomWidth,
        int kingdomDepth,
        string? expected
    )
    {
        // Arrange - the kingdoms in the corners, those at the sides turned (playtest 2026-11-15, H3)
        BoardSizeDto board = new(width, height, kingdomWidth, kingdomDepth);

        // Act
        string? problem = BoardFit.ProblemOf(board);

        // Assert
        Assert.True(
            expected is null ? problem is null : problem?.Contains(expected, StringComparison.Ordinal) == true,
            problem
        );
    }
}
