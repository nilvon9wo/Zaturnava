using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

public sealed class LearningTest
{
    private static readonly Chaturaji2027Rules Rules = new();

    private readonly StringWriter _progress = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Learn_WhenGivenGames_LearnsFromEveryPositionOfThoseFinishedInTheRuleset()
    {
        // Arrange - a finished game, and it drawn; and a blank line, an unfinished game, and another ruleset's
        string finished = (await Played(4, 10000)).First(line => !line.Contains("\"outcome\":null"));
        GameHistory won = JsonSerializer.Deserialize<GameHistory>(finished, JsonSerializerOptions.Web)!;
        string drawn = JsonSerializer.Serialize(
            won with { Outcome = won.Outcome! with { Winners = [] } },
            JsonSerializerOptions.Web
        );
        string[] histories =
        [
            finished, drawn, string.Empty, (await Played(1, 3))[0],
            finished.Replace("chaturaji-2027", "other", StringComparison.Ordinal),
        ];
        string alone = await this.ProgressOf([finished]);

        // Act
        ValueNetworkData network = await new Learning(Rules, this._progress)
            .Learn(histories, 1, 4, new Random(7), this._cancellation);

        // Assert
        Assert.Equal(
            (Rules.Id, new PositionEncoder(Rules).Size, 4, 2 * PositionsIn(alone), true),
            (
                network.RulesetId,
                network.Inputs,
                network.Hidden,
                PositionsIn(this._progress.ToString()),
                drawn.Contains("\"winners\":[]", StringComparison.Ordinal)
            )
        );
        Assert.True(PositionsIn(alone) > 0, alone);
    }

    [Fact]
    public async Task Learn_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        // Act
        Task learning = new Learning(Rules, this._progress).Learn([], 1, 4, new Random(7), cancelled.Token);

        // Assert
        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => learning);
    }

    private async Task<string> ProgressOf(string[] histories)
    {
        StringWriter progress = new();
        _ = await new Learning(Rules, progress).Learn(histories, 1, 4, new Random(7), this._cancellation);
        return progress.ToString();
    }

    /// <summary>
    /// Games Argus plays itself, of at most so many steps, as their histories' lines: most end long before ten
    /// thousand, but nothing makes every game end.
    /// </summary>
    private static async Task<string[]> Played(int games, int steps)
    {
        StringWriter output = new();
        string[] args =
            ["--games", $"{games}", "--max-steps", $"{steps}", "--bots", "argus,argus,argus,argus"];
        _ = await SelfPlayCommand.Run(args, output, new StringWriter(), CancellationToken.None);
        return output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>How many positions the progress says each epoch went through: "epoch 1: 120 positions, ...".</summary>
    private static int PositionsIn(string progress) =>
        int.Parse(progress.Split(' ')[2], System.Globalization.CultureInfo.InvariantCulture);
}
