namespace Net.NowhereAtAll.Zaturnava.Contracts.Test;

public sealed class GameRoutesTest
{
    [Fact]
    public void Game_WhenTheIdNeedsEscaping_EscapesIt()
    {
        // Arrange - nothing beyond the id

        // Act
        string route = GameRoutes.Game("a/b c");

        // Assert
        Assert.Equal("api/games/a%2Fb%20c", route);
    }

    [Fact]
    public void Seat_WhenGivenASeat_RoutesUnderItsGame()
    {
        // Arrange - nothing beyond the arguments

        // Act
        string route = GameRoutes.Seat("g", 3);

        // Assert
        Assert.Equal("api/games/g/seats/3", route);
    }

    [Fact]
    public void Actions_WhenGivenAGame_RoutesUnderIt()
    {
        // Arrange - nothing beyond the id

        // Act
        string route = GameRoutes.Actions("g");

        // Assert
        Assert.Equal("api/games/g/actions", route);
    }

    [Fact]
    public void Advance_WhenGivenAGame_RoutesUnderIt()
    {
        // Arrange - nothing beyond the id

        // Act
        string route = GameRoutes.Advance("g");

        // Assert
        Assert.Equal("api/games/g/advance", route);
    }
}
