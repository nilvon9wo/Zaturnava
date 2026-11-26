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
            Opener = 2,
            IsLostWithoutLeaders = true,
            IdleSeconds = 90,
            StartRows = 3,
            FaceOffPause = 30,
            Board = new BoardSizeDto(12, 10, 4, 3),
            TerrainMask = 7,
            TerrainValue = 5,
        };

        // Act
        GameSetup? parsed = GameSetup.Parse(setup.Text);

        // Assert
        Assert.Equal(
            (
                "grand-legions-custom;0;1,1,1,1;,,,;armies=draft-own,budget=150,secret=1,hidden=1,players=1,side=1,"
                    + "dice-off=1,idle=5,opener=2,leaders=1,turn-time=90,start-rows=3,face-off-pause=30,terrain=7:5,"
                    + "board=12x10x4x3",
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
    [InlineData("a;0;1;;opener=-1")]
    [InlineData("a;0;1;;opener=many")]
    [InlineData("a;0;1;;terrain=7")]
    [InlineData("a;0;1;;terrain=7:many")]
    [InlineData("a;0;1;;terrain=many:7")]
    [InlineData("a;0;1;;turn-time=0")]
    [InlineData("a;0;1;;turn-time=many")]
    [InlineData("a;0;1;;start-rows=0")]
    [InlineData("a;0;1;;start-rows=many")]
    [InlineData("a;0;1;;face-off-pause=0")]
    [InlineData("a;0;1;;face-off-pause=many")]
    public void Parse_WhenAChoiceCannotBeRead_LeavesItUnmade(string text)
    {
        // Act
        GameSetup? parsed = GameSetup.Parse(text);

        // Assert
        Assert.Equal(
            (null, null, null, null, 0, 0, null, null, null),
            (parsed?.Board, parsed?.Budget, parsed?.IdleRounds, parsed?.Opener, parsed?.TerrainMask,
                parsed?.TerrainValue, parsed?.IdleSeconds, parsed?.StartRows, parsed?.FaceOffPause)
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
