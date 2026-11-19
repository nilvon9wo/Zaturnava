using Net.NowhereAtAll.Zaturnava.Contracts;

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

    [Fact]
    public void Text_WhenItsOtherChoicesAreMade_KeepsThemToParseBack()
    {
        // Arrange
        GameSetup setup = new("grand-legions-custom", 0, [1, 1, 1, 1], ["", "", "", ""])
        {
            Armies = "draft-own",
            Budget = 150,
            IsSecret = true,
            IsHiddenFromAllies = true,
            IsForPlayers = true,
            PromotesAtSide = true,
            AreDiceOff = true,
            IdleRounds = 5,
            Board = new BoardSizeDto(12, 10, 4, 3),
        };

        // Act
        GameSetup? parsed = GameSetup.Parse(setup.Text);

        // Assert
        Assert.Equal(
            (
                "grand-legions-custom;0;1,1,1,1;,,,;armies=draft-own,budget=150,secret=1,hidden=1,players=1,side=1,"
                    + "dice-off=1,idle=5,board=12x10x4x3",
                setup with { Levels = parsed!.Levels, Picks = parsed.Picks }
            ),
            (setup.Text, parsed)
        );
    }

    [Theory]
    [InlineData("a;0;1;;board=12x10x4")]
    [InlineData("a;0;1;;board=12x0x4x4")]
    [InlineData("a;0;1;;board")]
    [InlineData("a;0;1;;budget=many")]
    [InlineData("a;0;1;;idle=0")]
    [InlineData("a;0;1;;idle=many")]
    public void Parse_WhenAChoiceCannotBeRead_LeavesItUnmade(string text)
    {
        // Act
        GameSetup? parsed = GameSetup.Parse(text);

        // Assert
        Assert.Equal((null, null, null), (parsed?.Board, parsed?.Budget, parsed?.IdleRounds));
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
