using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

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
        // Arrange - Argus ends most games long before ten thousand steps; a win is a team's, a draw no one's
        string[] args = ["--games", "4", "--max-steps", "10000", "--bots", "argus,argus,argus,argus"];

        // Act
        _ = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        JsonElement outcome = this._output.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line).RootElement.GetProperty("outcome"))
            .First(ended => ended.ValueKind != JsonValueKind.Null);
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

    [Fact]
    public async Task Run_WhenLearning_WritesTheNetworkAndHowEachEpochWent()
    {
        // Arrange - a game Argus finishes, to learn from in two epochs
        string file = Path.GetTempFileName();
        string[] play = ["--games", "1", "--max-steps", "10000", "--bots", "argus,argus,argus,argus", "--out", file];
        _ = await SelfPlayCommand.Run(play, this._output, this._errors, this._cancellation);
        string[] args = ["--learn", file, "--epochs", "2", "--hidden", "4"];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        File.Delete(file);
        ValueNetworkData? network =
            JsonSerializer.Deserialize<ValueNetworkData>(this._output.ToString(), JsonSerializerOptions.Web);
        Assert.Equal(
            (SelfPlayCommand.Succeeded, "chaturaji-2027", 4, 2),
            (
                exitCode,
                network?.RulesetId,
                network?.Hidden,
                this._errors.ToString().Split('\n').Count(line => line.StartsWith("epoch ", StringComparison.Ordinal))
            )
        );
    }

    [Fact]
    public async Task Run_WhenLearningFromANetwork_GoesOnFromIt()
    {
        // Arrange - a game Argus finishes, and a network of 3 hidden units to go on from
        string games = Path.GetTempFileName();
        string start = Path.GetTempFileName();
        string[] play = ["--games", "1", "--max-steps", "10000", "--bots", "argus,argus,argus,argus", "--out", games];
        _ = await SelfPlayCommand.Run(play, this._output, this._errors, this._cancellation);
        await File.WriteAllTextAsync(start, NetworkJson(Size), this._cancellation);
        string[] args = ["--learn", games, "--from", start, "--epochs", "1", "--hidden", "4", "--lambda", "0.5"];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        File.Delete(games);
        File.Delete(start);
        ValueNetworkData? network =
            JsonSerializer.Deserialize<ValueNetworkData>(this._output.ToString(), JsonSerializerOptions.Web);
        Assert.Equal((SelfPlayCommand.Succeeded, 3), (exitCode, network?.Hidden));
    }

    [Fact]
    public async Task Run_WhenGivenANetwork_AthenaPlaysByIt()
    {
        // Arrange
        string file = Path.GetTempFileName();
        await File.WriteAllTextAsync(file, NetworkJson(Size), this._cancellation);
        string[] args = ["--games", "1", "--max-steps", "6", "--bots", "athena,sage,athena,sage", "--network", file];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        File.Delete(file);
        JsonElement game = JsonDocument.Parse(this._output.ToString()).RootElement;
        Assert.Equal(
            (SelfPlayCommand.Succeeded, "athena,sage,athena,sage"),
            (exitCode, string.Join(',', game.GetProperty("seats").EnumerateArray().Select(Player)))
        );
    }

    [Theory]
    [InlineData("not json", "There is no network to read in ")]
    [InlineData("null", "")]
    [InlineData("{\"rulesetId\": \"other\"}", "")]
    [InlineData("small", "")]
    public async Task Run_WhenTheNetworkIsNotOneForTheRuleset_SaysSoAndExitsAsMisused(string content, string expected)
    {
        // Arrange - a network for positions of 2 numbers is no network for Chaturaji 2027's
        string file = Path.GetTempFileName();
        await File.WriteAllTextAsync(file, content == "small" ? NetworkJson(2) : content, this._cancellation);

        // Act
        int exitCode = await SelfPlayCommand.Run(["--network", file], this._output, this._errors, this._cancellation);

        // Assert
        File.Delete(file);
        string said = this._errors.ToString();
        Assert.Equal(
            (SelfPlayCommand.Misused, true),
            (exitCode, said.StartsWith(expected.Length > 0 ? expected : file, StringComparison.Ordinal))
        );
    }

    [Fact]
    public async Task Run_WhenTheNetworksFileIsNotThere_SaysSoAndExitsAsMisused()
    {
        // Arrange
        string[] args = ["--from", Path.Combine(Path.GetTempPath(), $"zaturnava-{Guid.NewGuid():N}.json")];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        Assert.Equal(
            (SelfPlayCommand.Misused, true),
            (exitCode, this._errors.ToString().StartsWith("There is no network to read in ", StringComparison.Ordinal))
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

    /// <summary>How many numbers a Chaturaji 2027 position is.</summary>
    private static int Size => new PositionEncoder(new Chaturaji2027Rules()).Size;

    /// <summary>A Chaturaji 2027 network of 3 hidden units, for positions of so many numbers, as JSON.</summary>
    private static string NetworkJson(int inputs) =>
        JsonSerializer.Serialize(
            ValueNetwork.Untrained(inputs, 3, new Random(7)).ToData("chaturaji-2027"),
            JsonSerializerOptions.Web
        );
}
