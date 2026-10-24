using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Games;

public sealed class SeatTokensTest
{
    [Fact]
    public void Issue_WhenCalledTwice_GivesDifferentThirtyTwoByteTokens()
    {
        // Arrange
        string first = SeatTokens.Issue();

        // Act
        string second = SeatTokens.Issue();

        // Assert
        Assert.Equal((64, 64, false), (first.Length, second.Length, first == second));
    }

    [Fact]
    public void Hash_WhenGivenTheSameToken_GivesTheSameSha256()
    {
        // Arrange - nothing beyond the arguments

        // Act
        string hash = SeatTokens.Hash("abc");

        // Assert
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", hash);
    }
}
