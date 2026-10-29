using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Bots;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

public sealed class SelfPlayCommandTest
{
    private readonly StringWriter _output = new();
    private readonly StringWriter _errors = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Run_WhenGivenGames_WritesEachAsOneLineOfJson()
    {
        // Arrange
        string[] args = ["--games", "3", "--max-steps", "5"];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        string[] lines = this._output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        JsonElement first = JsonDocument.Parse(lines[0]).RootElement;
        Assert.Equal(
            (SelfPlayCommand.Succeeded, 3, "chaturaji-2027", 4, "random", 5, JsonValueKind.Null),
            (
                exitCode,
                lines.Length,
                first.GetProperty("rulesetId").GetString(),
                first.GetProperty("seats").GetArrayLength(),
                first.GetProperty("seats")[0].GetProperty("player").GetString(),
                first.GetProperty("actions").EnumerateArray()
                    .Count(action => action.GetProperty("kind").GetString() != "dice"),
                first.GetProperty("outcome").ValueKind
            )
        );
    }

    [Fact]
    public async Task Run_WhenAGameEnds_SaysWhoWonItAndHow()
    {
        // Arrange - Argus ends its games long before ten thousand steps; a win is a team's, a draw no one's
        string[] args = ["--games", "1", "--max-steps", "10000", "--bots", "argus,argus,argus,argus"];

        // Act
        _ = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        JsonElement game = JsonDocument.Parse(this._output.ToString()).RootElement;
        JsonElement outcome = game.GetProperty("outcome");
        int winners = outcome.GetProperty("winners").GetArrayLength();
        Assert.Equal((true, true), (winners is 0 or 2, outcome.GetProperty("ending").GetString()!.Length > 0));
    }

    [Fact]
    public async Task Run_WhenGivenABotAssembly_SeatsItsBotsBesideOurs()
    {
        // Arrange - this assembly, with its own bots
        string[] args =
        [
            "--games", "1", "--max-steps", "20", "--bots", "seeded-own,sage,plain-own,random",
            "--bot-assembly", typeof(SeededOwnBot).Assembly.Location,
        ];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        JsonElement game = JsonDocument.Parse(this._output.ToString()).RootElement;
        Assert.Equal(
            (SelfPlayCommand.Succeeded, "seeded-own,sage,plain-own,random"),
            (exitCode, string.Join(',', game.GetProperty("seats").EnumerateArray().Select(Player)))
        );
    }

    [Fact]
    public async Task Run_WhenTheBotAssemblyCannotBeLoaded_SaysSoAndExitsAsMisused()
    {
        // Arrange
        string[] args = ["--bot-assembly", Path.Combine(Path.GetTempPath(), $"zaturnava-{Guid.NewGuid():N}.dll")];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        Assert.Equal(
            (SelfPlayCommand.Misused, true),
            (exitCode, this._errors.ToString().StartsWith("No bots could be loaded", StringComparison.Ordinal))
        );
    }

    [Fact]
    public async Task Run_WhenGivenAFile_WritesTheGamesThere()
    {
        // Arrange
        string file = Path.Combine(Path.GetTempPath(), $"zaturnava-{Guid.NewGuid():N}.jsonl");
        string[] args = ["--games", "2", "--max-steps", "3", "--bots", "random,random,random,random", "--out", file];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        string[] lines = await File.ReadAllLinesAsync(file, this._cancellation);
        File.Delete(file);
        Assert.Equal((SelfPlayCommand.Succeeded, 2, string.Empty), (exitCode, lines.Length, this._output.ToString()));
    }

    [Fact]
    public async Task Run_WhenTraining_WritesTheTunedWeightsAndHowEachNudgeWent()
    {
        // Arrange - one round, a game a side for each nudge, two steps a game, from what Sage has learned
        string[] args = ["--train", "1", "--games", "1", "--max-steps", "2"];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        SageWeights? tuned =
            JsonSerializer.Deserialize<SageWeights>(this._output.ToString(), JsonSerializerOptions.Web);
        Assert.Equal(
            (SelfPlayCommand.Succeeded, true, true),
            (
                exitCode,
                tuned is not null,
                this._errors.ToString().StartsWith("round 1, score ", StringComparison.Ordinal)
            )
        );
    }

    [Theory]
    [InlineData("--ruleset", "checkers", "There is no 'checkers' ruleset.")]
    [InlineData("--bots", "random,genius,random,random", "There is no 'genius' bot.")]
    [InlineData("--games", "none", "--games must be a whole number above 0")]
    public async Task Run_WhenMisused_SaysWhyAndExitsWithMisused(string name, string value, string expected)
    {
        // Arrange - nothing beyond the arguments

        // Act
        int exitCode = await SelfPlayCommand.Run([name, value], this._output, this._errors, this._cancellation);

        // Assert
        Assert.Equal(
            (SelfPlayCommand.Misused, true),
            (exitCode, this._errors.ToString().StartsWith(expected, StringComparison.Ordinal))
        );
    }

    private static string? Player(JsonElement seat) => seat.GetProperty("player").GetString();
}
