namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class GameSetupTest
{
    [Fact]
    public void Text_WhenParsedBack_IsTheSameSetup()
    {
        // Arrange
        GameSetup setup = new("chaturaji-2027", 2, [1, -2, -1, 2], ["", "", "", "oracle"]);

        // Act
        GameSetup? parsed = GameSetup.Parse(setup.Text);

        // Assert
        Assert.Equal(
            ("chaturaji-2027;2;1,-2,-1,2;,,,oracle", "chaturaji-2027", 2, "1,-2,-1,2", ",,,oracle"),
            (
                setup.Text,
                parsed?.RulesetId,
                parsed?.Colour,
                string.Join(',', parsed?.Levels ?? []),
                string.Join(',', parsed?.Picks ?? [])
            )
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("chaturaji-2027;2")]
    [InlineData("chaturaji-2027;two;1,-2,-1,2;,,,")]
    [InlineData("chaturaji-2027;2;1,x,-1,2;,,,")]
    public void Parse_WhenNotASetup_IsNone(string? text)
    {
        // Arrange - nothing kept, or not as a setup is

        // Act
        GameSetup? parsed = GameSetup.Parse(text);

        // Assert
        Assert.Null(parsed);
    }
}
