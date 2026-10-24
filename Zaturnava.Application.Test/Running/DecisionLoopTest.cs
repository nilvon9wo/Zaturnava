using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Running;

public sealed class DecisionLoopTest
{
    private const int GameLength = 5;

    private readonly TinyRules _rules = new(GameLength);
    private readonly ImmutableList<IDeciding> _players = [new FirstChoiceBot(), new FirstChoiceBot()];
    private readonly LoadedDice _dice = new(4);
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task PlayAll_WhenThereAreStepsToSpare_PlaysUntilTheGameIsOver()
    {
        // Arrange - nothing beyond the rules and players

        // Act
        Played played = await DecisionLoop.PlayAll(this._rules, this._players, this._dice, 100, this._cancellation);

        // Assert
        Assert.Equal((GameLength, null), (played.Actions.Count, played.State.DecidingSeat));
    }

    [Fact]
    public async Task PlayAll_WhenTheStepsRunOut_StopsThere()
    {
        // Arrange - nothing beyond the rules and players

        // Act
        Played played = await DecisionLoop.PlayAll(this._rules, this._players, this._dice, 2, this._cancellation);

        // Assert
        Assert.Equal(2, played.Actions.Count);
    }

    [Fact]
    public async Task PlayWhile_WhenTheDecidingSeatHasNoDecider_StopsAtOnce()
    {
        // Arrange
        Played start = new(this._rules.CreateInitialState(), []);

        // Act
        Played played =
            await DecisionLoop.PlayWhile(this._rules, start, _ => null, this._dice, 100, this._cancellation);

        // Assert
        Assert.Same(start, played);
    }

    [Fact]
    public async Task PlayAll_WhenTheGameWaitsOnDice_RollsThemForTheSeat()
    {
        // Arrange
        RollingRules rules = new(2);

        // Act
        Played played = await DecisionLoop.PlayAll(rules, this._players, this._dice, 100, this._cancellation);

        // Assert
        Assert.Equal(
            ["roll", "dice 4", "roll", "dice 4"],
            played.Actions.Select(action => $"{action.Kind} {action.Arguments.GetValueOrDefault("values")}".Trim())
        );
    }

    [Fact]
    public async Task PlayWhile_WhenAPersonRolled_RollsTheirDiceAndThenWaitsForThem()
    {
        // Arrange
        RollingRules rules = new(2);
        GameState rolling = rules.CreateInitialState() with { AwaitedDice = new DiceRequest(1, 6) };
        Played start = new(rolling, [new RollAction(new SeatId(0)).Describe()]);

        // Act
        Played played = await DecisionLoop.PlayWhile(rules, start, _ => null, this._dice, 100, this._cancellation);

        // Assert
        Assert.Equal(
            (2, new SeatId(1), 4),
            (played.Actions.Count, played.State.DecidingSeat, played.State.Scores[new SeatId(0)])
        );
    }

    [Fact]
    public async Task PlayWhile_WhenNoDecisionsAreLeftButDiceAreAwaited_StillRollsThem()
    {
        // Arrange
        RollingRules rules = new(2);
        GameState rolling = rules.CreateInitialState() with { AwaitedDice = new DiceRequest(1, 6) };
        Played start = new(rolling, [new RollAction(new SeatId(0)).Describe()]);

        // Act
        Played played = await DecisionLoop.PlayWhile(
            rules,
            start,
            _ => new FirstChoiceBot(),
            this._dice,
            0,
            this._cancellation
        );

        // Assert
        Assert.Equal((2, new SeatId(1)), (played.Actions.Count, played.State.DecidingSeat));
    }

    [Fact]
    public async Task PlayAll_WhenCancelledWhileDiceAreAwaited_StopsWithoutRolling()
    {
        // Arrange
        RollingRules rules = new(2);
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        // Act
        Task<Played> playing = DecisionLoop.PlayAll(rules, this._players, this._dice, 100, cancelled.Token);

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => playing);
    }
}
