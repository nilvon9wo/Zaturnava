using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Bots;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

public sealed class TrainingTest
{
    private readonly StringWriter _progress = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Tune_WhenANudgeWinsClearly_KeepsIt()
    {
        // Arrange - raising any weight wins 6 to 4, but raising leaders loses; lowering it wins
        Training training = new(
            (candidate, current, _) => Task.FromResult(
                (candidate.Leaders, current.Leaders) switch
                {
                    ( > 2, _) => (3, 7),
                    ( < 2, 2) => (8, 2),
                    _ => (6, 4),
                }
            ),
            this._progress
        );

        // Act
        SageWeights tuned = await training.Tune(SageWeights.Default, 1, this._cancellation);

        // Assert
        Assert.Equal(
            ImmutableList.Create(1.3, 1.3, 1.4, 0.65, 3.9, 1.3, 0.39),
            [.. tuned.Values.Select(value => Math.Round(value, 6))]
        );
        Assert.Contains(
            "round 1, leaders 2 to 1.4: won 8, lost 2, kept",
            this._progress.ToString(),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task Tune_WhenNoNudgeWinsClearly_LeavesTheWeightsAndSaysSo()
    {
        // Arrange - every nudge wins by less than a tenth of the games won or lost
        Training training = new((_, _, _) => Task.FromResult((10, 9)), this._progress);

        // Act
        SageWeights tuned = await training.Tune(SageWeights.Default, 2, this._cancellation);

        // Assert
        Assert.Equal(SageWeights.Default, tuned);
        Assert.Equal(
            (28, "round 1, score 1 to 1.3: won 10, lost 9"),
            (this.Lines().Length, this.Lines()[0])
        );
    }

    [Fact]
    public async Task Tune_WhenAWeightIsNothing_TriesItAtSomething()
    {
        // Arrange - a weight of nothing can only be raised from nothing
        SageWeights start = SageWeights.Default with { Chances = 0 };
        Training training = new(
            (candidate, _, _) => Task.FromResult(candidate.Chances > 0 ? (9, 1) : (1, 9)),
            this._progress
        );

        // Act
        SageWeights tuned = await training.Tune(start, 1, this._cancellation);

        // Assert
        Assert.Equal(0.3, tuned.Chances);
    }

    private string[] Lines() =>
        this._progress.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
}
