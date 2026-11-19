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
    public void InvitationTo_WhenAsked_NamesTheGameThePageAndTheCode()
    {
        // Arrange
        const string gameId = "3fa2c9d1e8b04f6a9c2d7e5b1a0f4c3d";

        // Act
        string invitation = GameCode.InvitationTo(gameId, "Chaturaji 2027");

        // Assert
        Assert.Equal(
            "Join my game of Chaturaji 2027: open https://nilvon9wo.itch.io/zaturnava and enter the game code "
                + "3fa2c9d1 in the lobby.",
            invitation
        );
    }
}
