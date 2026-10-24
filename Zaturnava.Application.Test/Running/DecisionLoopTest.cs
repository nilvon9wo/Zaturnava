using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Running;

public sealed class DecisionLoopTest
{
    private const int GameLength = 5;

    private readonly TinyRules _rules = new(GameLength);
    private readonly ImmutableList<IDeciding> _players = [new FirstChoiceBot(), new FirstChoiceBot()];
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task PlayAll_WhenThereAreStepsToSpare_PlaysUntilTheGameIsOver()
    {
        // Arrange - nothing beyond the rules and players

        // Act
        Played played = await DecisionLoop.PlayAll(this._rules, this._players, 100, this._cancellation);

        // Assert
        Assert.Equal((GameLength, null), (played.Actions.Count, played.State.DecidingSeat));
    }

    [Fact]
    public async Task PlayAll_WhenTheStepsRunOut_StopsThere()
    {
        // Arrange - nothing beyond the rules and players

        // Act
        Played played = await DecisionLoop.PlayAll(this._rules, this._players, 2, this._cancellation);

        // Assert
        Assert.Equal(2, played.Actions.Count);
    }

    [Fact]
    public async Task PlayWhile_WhenTheDecidingSeatHasNoDecider_StopsAtOnce()
    {
        // Arrange
        Played start = new(this._rules.CreateInitialState(), []);

        // Act
        Played played = await DecisionLoop.PlayWhile(this._rules, start, _ => null, 100, this._cancellation);

        // Assert
        Assert.Same(start, played);
    }
}
