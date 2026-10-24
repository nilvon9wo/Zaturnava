using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Games;

public sealed class GameServiceTest
{
    private const int GameLength = 6;
    private const int AtOnce = 100;
    private static readonly SeatId First = new(0);
    private static readonly SeatId Second = new(1);
    private static readonly SeatPlan Bot = new(FirstChoiceBot.BotId);

    private readonly RecordingNotifier _notifier = new();
    private readonly GameService _service;
    private readonly GameService _paced;
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    /// <summary>Two services: one whose bots reply at once, one whose bots wait to be advanced.</summary>
    public GameServiceTest()
    {
        this._service = this.Service(new GameServiceSettings(AtOnce));
        this._paced = this.Service(GameServiceSettings.Default);
    }

    [Fact]
    public void Rulesets_WhenListed_AreTheRegisteredOnes()
    {
        // Arrange - nothing beyond the service

        // Act
        ImmutableList<IRulesApplying> rulesets = this._service.Rulesets;

        // Assert
        Assert.Equal([TinyRules.RulesetId, RollingRules.RulesetId], rulesets.Select(rules => rules.Id));
    }

    [Fact]
    public void Bots_WhenListed_AreTheRegisteredOnes()
    {
        // Arrange - nothing beyond the service

        // Act
        ImmutableList<BotProfile> bots = this._service.Bots;

        // Assert
        Assert.Equal([FirstChoiceBot.BotId], bots.Select(bot => bot.Id));
    }

    [Fact]
    public async Task Create_WhenTheRulesetIsUnknown_ThrowsUnknownRulesetException()
    {
        // Arrange - nothing beyond the service

        // Act
        Task<GameId> creating = this._service.Create("chess", [SeatPlan.Open, SeatPlan.Open], this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<UnknownRulesetException>(() => creating);
    }

    [Fact]
    public async Task Create_WhenNotEverySeatHasAPlan_ThrowsInvalidGameSetupException()
    {
        // Arrange - nothing beyond the service

        // Act
        Task<GameId> creating = this._service.Create(TinyRules.RulesetId, [SeatPlan.Open], this._cancellation);

        // Assert
        InvalidGameSetupException exception = await Assert.ThrowsAsync<InvalidGameSetupException>(() => creating);
        Assert.Equal("The game has 2 seats, not 1.", exception.Message);
    }

    [Fact]
    public async Task Create_WhenTheFirstSeatIsABot_LetsItPlayUntilAPersonMustDecide()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [Bot, SeatPlan.Open], this._cancellation);

        // Act
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);

        // Assert
        Assert.Equal((1, Second), (snapshot.Record.Actions.Count, snapshot.State.DecidingSeat));
    }

    [Fact]
    public async Task Create_WhenEverySeatIsABot_PlaysTheWholeGame()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [Bot, Bot], this._cancellation);

        // Act
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);

        // Assert
        Assert.Equal((GameLength, null), (snapshot.Record.Actions.Count, snapshot.State.DecidingSeat));
    }

    [Fact]
    public async Task List_WhenAGameWasCreated_IncludesIt()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);

        // Act
        ImmutableList<GameRecord> games = await this._service.List(this._cancellation);

        // Assert
        Assert.Equal([id], games.Select(game => game.Id));
    }

    [Fact]
    public async Task Get_WhenTheGameIsUnknown_ThrowsGameNotFoundException()
    {
        // Arrange - nothing beyond the service

        // Act
        Task<GameSnapshot> getting = this._service.Get(new GameId("missing"), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<GameNotFoundException>(() => getting);
    }

    [Fact]
    public async Task Claim_WhenTheSeatIsOpen_ReturnsATokenThatHoldsIt()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);

        // Act
        string token = await this._service.Claim(id, First, this._cancellation);

        // Assert
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);
        Assert.True(snapshot.Record.Controllers[0].IsHeldBy(token));
    }

    [Fact]
    public async Task Claim_WhenClaimed_NotifiesTheChange()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);

        // Act
        _ = await this._service.Claim(id, First, this._cancellation);

        // Assert
        Assert.Equal([(id, 1)], this._notifier.Changes);
    }

    [Fact]
    public async Task Claim_WhenTheSeatIsABots_ThrowsSeatUnavailableException()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);

        // Act
        Task<string> claiming = this._service.Claim(id, Second, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<SeatUnavailableException>(() => claiming);
    }

    [Fact]
    public async Task Claim_WhenTheSeatDoesNotExist_ThrowsSeatUnavailableException()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);

        // Act
        Task<string> claiming = this._service.Claim(id, new SeatId(5), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<SeatUnavailableException>(() => claiming);
    }

    [Fact]
    public async Task Submit_WhenTheTokenHoldsTheDecidingSeat_AppliesTheActionAndLetsBotsReply()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);
        string token = await this._service.Claim(id, First, this._cancellation);

        // Act
        GameSnapshot snapshot = await this._service.Submit(id, token, Pass(First), this._cancellation);

        // Assert
        Assert.Equal(
            (2, First, 2),
            (snapshot.Record.Actions.Count, snapshot.State.DecidingSeat, snapshot.Record.Version)
        );
    }

    [Fact]
    public async Task Submit_WhenThePlayerRolls_RollsTheirDiceAtOnce()
    {
        // Arrange
        GameId id =
            await this._service.Create(RollingRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await this._service.Claim(id, First, this._cancellation);
        ActionDescriptor roll = new RollAction(First).Describe();

        // Act
        GameSnapshot snapshot = await this._service.Submit(id, token, roll, this._cancellation);

        // Assert
        Assert.Equal(
            ("roll dice", Second, 4),
            (
                string.Join(' ', snapshot.Record.Actions.Select(action => action.Kind)),
                snapshot.State.DecidingSeat,
                snapshot.State.Scores[First]
            )
        );
    }

    [Fact]
    public async Task Submit_WhenTheActionIsHowDiceCameUp_ThrowsIllegalActionException()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await this._service.Claim(id, First, this._cancellation);
        ActionDescriptor sixes = new DiceAction(First, [6, 6]).Describe();

        // Act
        Task<GameSnapshot> submitting = this._service.Submit(id, token, sixes, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<IllegalActionException>(() => submitting);
    }

    [Fact]
    public async Task Submit_WhenTheTokenDoesNotHoldTheSeat_ThrowsNotYourTurnException()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        _ = await this._service.Claim(id, First, this._cancellation);

        // Act
        Task<GameSnapshot> submitting = this._service.Submit(id, "not the token", Pass(First), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourTurnException>(() => submitting);
    }

    [Fact]
    public async Task Submit_WhenAnotherSeatMustDecide_ThrowsNotYourTurnException()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await this._service.Claim(id, Second, this._cancellation);

        // Act
        Task<GameSnapshot> submitting = this._service.Submit(id, token, Pass(Second), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourTurnException>(() => submitting);
    }

    [Fact]
    public async Task Submit_WhenTheGameIsOver_ThrowsNotYourTurnException()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);
        string token = await this._service.Claim(id, First, this._cancellation);
        _ = await this._service.Submit(id, token, Pass(First), this._cancellation);
        _ = await this._service.Submit(id, token, Pass(First), this._cancellation);
        _ = await this._service.Submit(id, token, Pass(First), this._cancellation);

        // Act
        Task<GameSnapshot> submitting = this._service.Submit(id, token, Pass(First), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourTurnException>(() => submitting);
    }

    [Fact]
    public async Task Submit_WhenTheRulesForbidTheAction_ThrowsIllegalActionException()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await this._service.Claim(id, First, this._cancellation);
        ActionDescriptor move = new MoveAction(First, new CellId(0), new CellId(1)).Describe();

        // Act
        Task<GameSnapshot> submitting = this._service.Submit(id, token, move, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<IllegalActionException>(() => submitting);
    }

    [Fact]
    public async Task Create_WhenBotsArePaced_LeavesTheFirstBotToBeAdvanced()
    {
        // Arrange
        GameId id = await this._paced.Create(TinyRules.RulesetId, [Bot, SeatPlan.Open], this._cancellation);

        // Act
        GameSnapshot snapshot = await this._paced.Get(id, this._cancellation);

        // Assert
        Assert.Equal((0, First), (snapshot.Record.Actions.Count, snapshot.State.DecidingSeat));
    }

    [Fact]
    public async Task Advance_WhenABotIsDeciding_LetsItMakeOneDecision()
    {
        // Arrange
        GameId id = await this._paced.Create(TinyRules.RulesetId, [Bot, Bot], this._cancellation);

        // Act
        GameSnapshot snapshot = await this._paced.Advance(id, 0, this._cancellation);

        // Assert
        Assert.Equal(
            (1, Second, 1),
            (snapshot.Record.Actions.Count, snapshot.State.DecidingSeat, snapshot.Record.Version)
        );
    }

    [Fact]
    public async Task Advance_WhenTheGameHasMovedOn_ChangesNothing()
    {
        // Arrange
        GameId id = await this._paced.Create(TinyRules.RulesetId, [Bot, Bot], this._cancellation);
        _ = await this._paced.Advance(id, 0, this._cancellation);

        // Act
        GameSnapshot snapshot = await this._paced.Advance(id, 0, this._cancellation);

        // Assert
        Assert.Equal((1, 1), (snapshot.Record.Actions.Count, snapshot.Record.Version));
    }

    [Fact]
    public async Task Advance_WhenAPersonIsDeciding_ChangesNothing()
    {
        // Arrange
        GameId id = await this._paced.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);

        // Act
        GameSnapshot snapshot = await this._paced.Advance(id, 0, this._cancellation);

        // Assert
        Assert.Equal((0, 0, 0), (snapshot.Record.Actions.Count, snapshot.Record.Version, this._notifier.Changes.Count));
    }

    [Fact]
    public async Task Get_WhenThePlayerHasRolled_TellsOfTheRoll()
    {
        // Arrange
        GameId id =
            await this._paced.Create(RollingRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await this._paced.Claim(id, First, this._cancellation);
        _ = await this._paced.Submit(id, token, new RollAction(First).Describe(), this._cancellation);

        // Act
        GameSnapshot snapshot = await this._paced.Get(id, this._cancellation);

        // Assert
        Assert.Equal([new DiceRolled(First, [4])], snapshot.Events);
    }

    private static ActionDescriptor Pass(SeatId seat) => new PassAction(seat).Describe();

    private GameService Service(GameServiceSettings settings) =>
        new(
            new RulesetCatalog([new TinyRules(GameLength), new RollingRules(GameLength)]),
            new BotCatalog([new FirstChoiceBot()]),
            new InMemoryGameStore(),
            this._notifier,
            new LoadedDice(4),
            settings
        );
}
