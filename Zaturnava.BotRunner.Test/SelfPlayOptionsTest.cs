namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

public sealed class SelfPlayOptionsTest
{
    [Fact]
    public void Parse_WhenGivenNothing_PlaysAHundredClassicGamesOfRandomBotsToStandardOutput()
    {
        // Arrange - nothing beyond the empty command line

        // Act
        SelfPlayOptions options = SelfPlayOptions.Parse([]);

        // Assert
        Assert.Equal(new SelfPlayOptions("classic", null, 100, 1000, null), options);
    }

    [Fact]
    public void Parse_WhenGivenEveryOption_TakesEach()
    {
        // Arrange
        string[] args =
        [
            "--ruleset", "tiny", "--bots", "random, first", "--games", "7", "--max-steps", "50", "--out", "games.jsonl",
        ];

        // Act
        SelfPlayOptions options = SelfPlayOptions.Parse(args);

        // Assert
        SelfPlayOptions expected = new("tiny", ["random", "first"], 7, 50, "games.jsonl");
        Assert.Equivalent(expected, options, strict: true);
    }

    [Theory]
    [InlineData("--colour", "red", "There is no --colour option.")]
    [InlineData("--games", "0", "--games must be a whole number above 0, not '0'.")]
    [InlineData("--max-steps", "many", "--max-steps must be a whole number above 0, not 'many'.")]
    public void Parse_WhenAnOptionIsWrong_SaysWhatIsWrongAndHowToUseIt(string name, string value, string expected)
    {
        // Arrange - nothing beyond the arguments

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => SelfPlayOptions.Parse([name, value]));

        // Assert
        Assert.Equal($"{expected} {SelfPlayOptions.Usage}", exception.Message);
    }

    [Fact]
    public void Parse_WhenTheLastOptionHasNoValue_SaysItNeedsOne()
    {
        // Arrange - nothing beyond the arguments

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => SelfPlayOptions.Parse(["--games"]));

        // Assert
        Assert.StartsWith("--games needs a value.", exception.Message, StringComparison.Ordinal);
    }
}
