using System.Collections.Immutable;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

public sealed class LearningTest
{
    private static readonly Chaturaji2027Rules Rules = new();
    private static readonly LearningPace Pace = new(1, 1, 0.02);

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
        Learning learning = new(Rules, this._progress);
        ValueNetworkData network = await learning
            .Learn(histories, learning.Start(4, new Random(7)), Pace, new Random(7), this._cancellation);

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
        Task learning = new Learning(Rules, this._progress)
            .Learn([], ValueNetwork.Untrained(1, 1, new Random(7)), Pace, new Random(7), cancelled.Token);

        // Assert
        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => learning);
    }

    [Fact]
    public async Task Start_WhenAsked_JudgesAsSageDoesUntilItLearns()
    {
        // Arrange - Sage's learned weights for the ruleset, and the position of a game's first steps that Sage
        // judges furthest from even, as one may be even for all a few steps tell
        PositionEncoder encoder = new(Rules);
        ImmutableList<(int Index, double Weight)> weighing = encoder.Weighing(SageWeights.Learned[Rules.Id]);
        (ImmutableList<(int Index, double Value)> input, double sum) = (await Positions())
            .Select(state => encoder.Encode(state, state.Seats[1].Id))
            .Select(encoded => (encoded, weighing
                .Join(
                    encoded,
                    weight => weight.Index,
                    value => value.Index,
                    (weight, value) => weight.Weight * value.Value
                )
                .Sum()
            ))
            .MaxBy(judged => Math.Abs(judged.Item2));

        // Act
        ValueNetwork network = new Learning(Rules, this._progress).Start(4, new Random(7));

        // Assert
        (double outcome, double margin) = network.Evaluate(input);
        Assert.Equal(Math.Tanh(sum / ValueNetwork.MarginScale), outcome, 1e-9);
        Assert.Equal(sum / ValueNetwork.MarginScale, margin, 1e-9);
        Assert.NotEqual(0, sum);
    }

    [Fact]
    public async Task Learn_WhenLambdaIsBelowOne_TeachesWhatTheNetworkJudgesOfWhatFollowsToo()
    {
        // Arrange - one game, taught from one start by how it went alone, and with what follows
        string[] finished = [(await Played(4, 10000)).First(line => !line.Contains("\"outcome\":null"))];
        Learning learning = new(Rules, this._progress);

        // Act
        ValueNetworkData alone = await learning.Learn(
            finished,
            learning.Start(4, new Random(7)),
            Pace,
            new Random(7),
            this._cancellation
        );
        ValueNetworkData mixed = await learning.Learn(
            finished,
            learning.Start(4, new Random(7)),
            Pace with { Lambda = 0.5 },
            new Random(7),
            this._cancellation
        );

        // Assert
        Assert.NotEqual(alone.DirectWeights, mixed.DirectWeights);
    }

    private async Task<string> ProgressOf(string[] histories)
    {
        StringWriter progress = new();
        Learning learning = new(Rules, progress);
        _ = await learning
            .Learn(histories, learning.Start(4, new Random(7)), Pace, new Random(7), this._cancellation);
        return progress.ToString();
    }

    /// <summary>Where a game Argus plays itself stands after each of its first few actions.</summary>
    private static async Task<ImmutableList<GameState>> Positions()
    {
        GameHistory history =
            JsonSerializer.Deserialize<GameHistory>((await Played(1, 60))[0], JsonSerializerOptions.Web)!;
        GameState start = Rules.CreateInitialState();
        return history.Actions.Aggregate(
            ImmutableList.Create(start),
            (sofar, action) => sofar.Add(Rules.Apply(sofar[^1], Rules.Parse(action)).State)
        );
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
