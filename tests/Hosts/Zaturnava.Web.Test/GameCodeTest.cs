namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class GameCodeTest
{
    [Theory]
    [InlineData("3fa2c9d1e8b04f6a9c2d7e5b1a0f4c3d", "3fa2c9d1")]
    [InlineData("3fa2c9d1", "3fa2c9d1")]
    [InlineData("g1", "g1")]
    public void Of_WhenAsked_IsTheFirstEightLettersOfTheIdOrAllOfAShortOne(string gameId, string expected)
    {
        // Arrange - as given

        // Act
        string code = GameCode.Of(gameId);

        // Assert
        Assert.Equal(expected, code);
    }

    [Fact]
    public void InvitationTo_WhenAsked_LinksToTheGameOnTheServersPageAndGivesItsCodeForItchIo()
    {
        // Arrange
        const string gameId = "3fa2c9d1e8b04f6a9c2d7e5b1a0f4c3d";

        // Act
        string invitation = GameCode.InvitationTo(gameId, "Chaturaji 2027", new Uri("https://games.example/"));

        // Assert
        Assert.Equal(
            "Join my game of Chaturaji 2027: https://games.example/play/?game=3fa2c9d1 (or open "
                + "https://nilvon9wo.itch.io/zaturnava and enter the game code 3fa2c9d1 in the lobby).",
            invitation
        );
    }

    [Theory]
    [InlineData("https://games.example/play/?game=3fa2c9d1", "3fa2c9d1")]
    [InlineData("https://games.example/play/?theme=dark&game=3f%20a2", "3f a2")]
    [InlineData("https://games.example/play/?game=", null)]
    [InlineData("https://games.example/play/?games", null)]
    [InlineData("https://games.example/play/", null)]
    public void InvitedBy_WhenThePagesAddressIsRead_IsTheCodeOfTheGameItGivesIfAny(string address, string? expected)
    {
        // Arrange - as given

        // Act
        string? code = GameCode.InvitedBy(address);

        // Assert
        Assert.Equal(expected, code);
    }
}
