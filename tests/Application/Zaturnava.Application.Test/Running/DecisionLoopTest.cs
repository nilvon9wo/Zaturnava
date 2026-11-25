using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
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
    public async Task PlayWhile_WhenAStepPausesTheGame_StopsAfterIt()
    {
        // Arrange - the second turn's step pauses the game, as a face-off a person may call opening would
        Played start = new(this._rules.CreateInitialState(), []);

        // Act
        Played played = await DecisionLoop.PlayWhile(
            this._rules,
            start,
            seat => this._players[seat.Value],
            this._dice,
            100,
            this._cancellation,
            (_, after) => after.TurnNumber == 3
        );

        // Assert
        Assert.Equal(2, played.Actions.Count);
    }

    [Fact]
    public async Task PlayAll_WhenTheRulesetDealsEachGame_DealsItFirstBySeedKeepingWhatItDealt()
    {
        // Arrange - the loaded dice roll the seed, 4, which the tiny rules score to the first seat; and, to the fourth,
        // how many seats were dealt a family: a bot of Makruk's pieces at the first, a decider no bot at the second
        // (so that a bot of a family is rated as it plays, 2026-11-20)
        TinyRules rules = new(GameLength, isDealt: true);
        ImmutableList<IDeciding> players = [new FirstChoiceBot(family: "Makruk"), new SeizingBot()];

        // Act
        Played played = await DecisionLoop.PlayAll(rules, players, this._dice, 100, this._cancellation);

        // Assert - what it dealt kept in the deal, so that the game replays as dealt however dealing changes
        Assert.Equal(
            ("deal", true, GameLength + 1, 4, 1),
            (
                played.Actions[0].Kind,
                ScenarioStart.IsKept(played.Actions),
                played.Actions.Count,
                played.State.Scores[new SeatId(0)],
                played.State.Scores[new SeatId(3)]
            )
        );
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
    public async Task PlayAll_WhenABotHasAChanceItTakes_TakesItAtOnceAndOnlyWhileItHolds()
    {
        // Arrange - the first seat's bot takes every chance; taking the face-off ends it
        TinyRules rules = new(GameLength, hasFaceOff: true);
        ImmutableList<IDeciding> players = [new SeizingBot(), new FirstChoiceBot()];

        // Act
        Played played = await DecisionLoop.PlayAll(rules, players, this._dice, 100, this._cancellation);

        // Assert
        Assert.Equal(
            (FaceOffAction.Kind, GameLength + 1, null),
            (played.Actions[0].Kind, played.Actions.Count, played.State.Position.PieceAt(new CellId(1)))
        );
    }

    [Fact]
    public async Task PlayAll_WhenABotLetsItsChanceGoBy_PlaysOnWithoutIt()
    {
        // Arrange
        TinyRules rules = new(GameLength, hasFaceOff: true);

        // Act
        Played played = await DecisionLoop.PlayAll(rules, this._players, this._dice, 100, this._cancellation);

        // Assert
        Assert.DoesNotContain(played.Actions, action => action.Kind == FaceOffAction.Kind);
    }

    [Fact]
    public async Task PlayWhile_WhenTheSeatWithAChanceHasNoDecider_LeavesItToThem()
    {
        // Arrange - a person plays the seat with the chance
        TinyRules rules = new(GameLength, hasFaceOff: true);
        Played start = new(rules.CreateInitialState(), []);

        // Act
        Played played = await DecisionLoop.PlayWhile(rules, start, _ => null, this._dice, 100, this._cancellation);

        // Assert
        Assert.Same(start, played);
    }

    [Theory]
    [InlineData(true, "move,pass,move,pass")]
    [InlineData(false, "move,move,move,move")]
    public async Task PlayAll_WhenABotWouldPutTheBoardBackAsTheTurnBegan_OffersItAnythingElseThereIs(
        bool mayPass,
        string expected
    )
    {
        // Arrange - a Rook a1 to b1 and back, taking nothing, is a pass with extra steps (the user's report of
        // 2026-11-20); the bot takes the first choice, a step, whenever it is offered; with no pass, the step back is
        // all there is
        ShuttleRules rules = new(2, mayPass);

        // Act
        Played played = await DecisionLoop.PlayAll(rules, [new FirstChoiceBot()], this._dice, 100, this._cancellation);

        // Assert
        Assert.Equal(expected, string.Join(",", played.Actions.Select(action => action.Kind)));
    }

    [Fact]
    public async Task PlayWhile_WhenCancelledWhileDiceAreAwaited_StopsWithoutRolling()
    {
        // Arrange
        RollingRules rules = new(2);
        Played start = new(rules.CreateInitialState(), []);
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        // Act
        Task<Played> playing = DecisionLoop.PlayWhile(
            rules,
            start,
            seat => this._players[seat.Value],
            this._dice,
            100,
            cancelled.Token
        );

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => playing);
    }

    [Fact]
    public async Task PlayAll_WhenCancelledWhileBotsDecide_StopsBeforeTheNextDecision()
    {
        // Arrange - rules with no dice, played by bots that never look at the token
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        // Act
        Task<Played> playing = DecisionLoop.PlayAll(this._rules, this._players, this._dice, 100, cancelled.Token);

        // Assert
        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => playing);
    }
}
