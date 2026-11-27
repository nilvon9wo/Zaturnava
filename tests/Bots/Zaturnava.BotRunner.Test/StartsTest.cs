using System.Text.Json;
using System.Text.Json.Nodes;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

/// <summary>Games started where games played before came down to few pieces (<see cref="Starts"/>).</summary>
public sealed class StartsTest
{
    private readonly StringWriter _output = new();
    private readonly StringWriter _errors = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Run_WhenGivenGamesToStartFrom_PlaysOnFromWhereEachCameDownToSoFewPieces()
    {
        // Arrange - a game Argus finishes; two games played on from where it came down to 30 pieces, with one game
        // of another ruleset and one that no longer replays beside it, which are left out
        string recorded = await this.Recorded();
        JsonObject other = JsonNode.Parse(recorded)!.AsObject();
        other["rulesetId"] = "another";
        JsonObject broken = JsonNode.Parse(recorded)!.AsObject();
        broken["actions"]!.AsArray().RemoveAt(0);
        string file = Path.GetTempFileName();
        await File.WriteAllLinesAsync(
            file,
            [other.ToJsonString(), broken.ToJsonString(), recorded],
            this._cancellation
        );
        string[] args =
        [
            "--games", "2", "--max-steps", "2", "--bots", "random,random,random,random", "--starts", file,
            "--pieces", "30",
        ];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert - each begins as the game recorded did, and goes on past where it began
        File.Delete(file);
        string[] lines = this._output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string opening = FirstActions(recorded, 3);
        Assert.Equal(
            (SelfPlayCommand.Succeeded, 2, true),
            (exitCode, lines.Length, lines.All(line => FirstActions(line, 3) == opening))
        );
    }

    [Fact]
    public async Task Run_WhenGivenGamesToStartFromAsTheServerGivesThem_ReadsThemAll()
    {
        // Arrange - the game recorded, in a JSON array, as the server gives its games
        string recorded = await this.Recorded();
        string file = Path.GetTempFileName();
        await File.WriteAllTextAsync(file, $"[{recorded}]", this._cancellation);
        string[] args = ["--games", "1", "--max-steps", "2", "--starts", file, "--pieces", "30", "--seed", "5"];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        File.Delete(file);
        Assert.Equal(
            (SelfPlayCommand.Succeeded, FirstActions(recorded, 3)),
            (exitCode, FirstActions(this._output.ToString().Trim(), 3))
        );
    }

    [Theory]
    [InlineData(true, "No chaturaji-2027 game in")]
    [InlineData(false, "There are no games to start from in")]
    public async Task Run_WhenNoGameGivenComesDownToSoFewPieces_SaysSo(bool exists, string expected)
    {
        // Arrange - the game recorded, which ends long before a single piece is left; or no file at all
        string file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jsonl");
        if (exists)
        {
            await File.WriteAllTextAsync(file, await this.Recorded(), this._cancellation);
        }

        string[] args = ["--games", "1", "--starts", file, "--pieces", "1"];

        // Act
        int exitCode = await SelfPlayCommand.Run(args, this._output, this._errors, this._cancellation);

        // Assert
        File.Delete(file);
        Assert.Equal(
            (SelfPlayCommand.Misused, true),
            (exitCode, this._errors.ToString().StartsWith(expected, StringComparison.Ordinal))
        );
    }

    /// <summary>A game Argus finishes, as one line of JSON.</summary>
    private async Task<string> Recorded()
    {
        StringWriter game = new();
        string[] play = ["--games", "1", "--max-steps", "10000", "--bots", "argus,argus,argus,argus"];
        _ = await SelfPlayCommand.Run(play, game, this._errors, this._cancellation);
        return game.ToString().Trim();
    }

    /// <summary>The game's first actions, as JSON.</summary>
    private static string FirstActions(string game, int count) =>
        string.Join(
            '|',
            JsonElement.Parse(game).GetProperty("actions").EnumerateArray()
                .Take(count)
                .Select(action => action.GetRawText())
        );
}
