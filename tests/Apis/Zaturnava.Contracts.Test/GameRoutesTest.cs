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
    public void History_WhenGivenAGame_RoutesUnderIt()
    {
        // Arrange - nothing beyond the id

        // Act
        string route = GameRoutes.History("g");

        // Assert
        Assert.Equal("api/games/g/history", route);
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

    [Fact]
    public void Fill_WhenGivenAGame_RoutesUnderIt()
    {
        // Arrange - nothing beyond the id

        // Act
        string route = GameRoutes.Fill("g");

        // Assert
        Assert.Equal("api/games/g/fill", route);
    }

    [Fact]
    public void Chat_WhenGivenAGame_RoutesUnderIt()
    {
        // Arrange - nothing beyond the id

        // Act
        string route = GameRoutes.Chat("g");

        // Assert
        Assert.Equal("api/games/g/chat", route);
    }

    [Fact]
    public void Chat_WhenGivenAMessageToReadAfter_AsksForWhatCameLater()
    {
        // Arrange - nothing beyond the arguments

        // Act
        string route = GameRoutes.Chat("g", 12);

        // Assert
        Assert.Equal("api/games/g/chat?after=12", route);
    }

    [Fact]
    public void SignIn_WhenGivenAServiceAndThePagesOrigin_EscapesBoth()
    {
        // Arrange - nothing beyond the arguments

        // Act
        string route = GameRoutes.SignIn("Git Hub", "https://html.itch.zone");

        // Assert
        Assert.Equal("signin/Git%20Hub?origin=https%3A%2F%2Fhtml.itch.zone", route);
    }

    [Fact]
    public void SignIn_WhenGivenACodeToLinkWith_AddsIt()
    {
        // Arrange - nothing beyond the arguments

        // Act
        string route = GameRoutes.SignIn("GitHub", "https://a.b", "c 0");

        // Assert
        Assert.Equal("signin/GitHub?origin=https%3A%2F%2Fa.b&link=c%200", route);
    }

    [Fact]
    public void Login_WhenGivenAService_RoutesUnderThePlayersLogins()
    {
        // Arrange - nothing beyond the service

        // Act
        string route = GameRoutes.Login("Git Hub");

        // Assert
        Assert.Equal("api/players/me/logins/Git%20Hub", route);
    }

    [Fact]
    public void Profile_WhenGivenAPlayersId_RoutesUnderThePlayers()
    {
        // Arrange - nothing beyond the id

        // Act
        string route = GameRoutes.Profile("p/1");

        // Assert
        Assert.Equal("api/players/p%2F1", route);
    }
}
