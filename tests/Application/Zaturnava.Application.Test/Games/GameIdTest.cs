using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Games;

public sealed class GameIdTest
{
    [Fact]
    public void New_WhenCalledTwice_GivesDifferentIds()
    {
        // Arrange
        GameId first = GameId.New();

        // Act
        GameId second = GameId.New();

        // Assert
        Assert.NotEqual(first, second);
    }
}
