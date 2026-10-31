using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
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
    private const string SecondFirst = "second-first";
    private const string Rolling = "rolling";
    private static readonly SeatId First = new(0);
    private static readonly SeatId Second = new(1);
    private static readonly SeatPlan Bot = new(FirstChoiceBot.BotId);

    private static readonly TimeSpan FiveSeconds = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Grace = GameServiceSettings.DefaultAwayGrace;

    private readonly RecordingNotifier _notifier = new();
    private readonly SteppedTime _time = new();
    private readonly GameService _service;
    private readonly GameService _paced;
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    /// <summary>Two services: one whose bots reply at once, one whose bots wait to be advanced.</summary>
    public GameServiceTest()
    {
        this._service = this.Service(GameServiceSettings.Default with { MaximumBotSteps = AtOnce });
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
    public async Task Create_WhenFromAScenario_StartsAndReplaysFromIt()
    {
        // Arrange
        GameId id = await this._paced.Create(
            TinyRules.RulesetId,
            [SeatPlan.Open, SeatPlan.Open],
            SecondFirst,
            this._cancellation
        );

        // Act
        GameSnapshot snapshot = await this._paced.Get(id, this._cancellation);

        // Assert
        Assert.Equal((ScenarioStart.Kind, Second), (snapshot.Record.Actions[0].Kind, snapshot.State.DecidingSeat));
        Assert.Equal([Rolling, SecondFirst], this._paced.Scenarios.Select(scenario => scenario.Id));
    }

    [Fact]
    public async Task Create_WhenTheScenarioIsForAnotherRuleset_ThrowsInvalidScenarioException()
    {
        // Arrange - nothing beyond the service

        // Act
        Task<GameId> creating =
            this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], Rolling, this._cancellation);

        // Assert
        InvalidScenarioException exception = await Assert.ThrowsAsync<InvalidScenarioException>(() => creating);
        Assert.Equal("The 'rolling' scenario is for the 'rolling' ruleset, not 'tiny'.", exception.Message);
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
    public async Task Create_WhenASeatIsOpen_WaitsForItWithNothingPlayed()
    {
        // Arrange - the bot would play first
        GameId id = await this._service.Create(TinyRules.RulesetId, [Bot, SeatPlan.Open], this._cancellation);

        // Act
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);

        // Assert
        Assert.Equal(
            (true, 0, 0, null),
            (snapshot.Record.IsWaiting, snapshot.Record.Actions.Count, snapshot.LegalActions.Count, snapshot.Away)
        );
    }

    [Fact]
    public async Task Claim_WhenTheLastOpenSeatIsTaken_BeginsTheGameLettingTheBotsPlayUntilAPersonMustDecide()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [Bot, SeatPlan.Open], this._cancellation);

        // Act
        _ = await this._service.Claim(id, Second, this._cancellation);

        // Assert
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);
        Assert.Equal(
            (false, 1, Second),
            (snapshot.Record.IsWaiting, snapshot.Record.Actions.Count, snapshot.State.DecidingSeat)
        );
    }

    [Fact]
    public async Task Claim_WhenAPlayerSignedInClaims_RemembersThemAtTheSeat()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);
        PlayerId player = new("ada");

        // Act
        _ = await this._service.Claim(id, First, player, "Impostor", this._cancellation);

        // Assert
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);
        HumanSeat seat = (HumanSeat)snapshot.Record.Controllers[0];
        Assert.Equal<(PlayerId?, string?)>((player, null), (seat.Claimant, seat.Nickname));
    }

    [Theory]
    [InlineData("  Ada  ", "Ada")]
    [InlineData("A nickname far too long to keep", "A nickname far too l")]
    [InlineData("Ad\u0007a", "Ada")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public async Task Claim_WhenAGuestGivesANickname_KeepsItCleanedAndCut(string? given, string? kept)
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);

        // Act
        _ = await this._service.Claim(id, First, null, given, this._cancellation);

        // Assert
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);
        Assert.Equal(kept, ((HumanSeat)snapshot.Record.Controllers[0]).Nickname);
    }

    [Fact]
    public async Task Submit_WhileASeatIsStillOpen_ThrowsNotYourTurnException()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await this._service.Claim(id, First, this._cancellation);

        // Act
        Task<GameSnapshot> submitting = this._service.Submit(id, token, Pass(First), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourTurnException>(() => submitting);
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
    public async Task Snapshots_WhenAGameIsOfARulesetNoLongerOffered_ReplaysOnlyTheOthers()
    {
        // Arrange
        InMemoryGameStore store = new();
        GameService service = this.Service(GameServiceSettings.Default, store);
        GameId id = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        GameRecord kept = (await store.Find(id, this._cancellation))!;
        await store.Add(kept with { Id = new GameId("retired"), RulesetId = "retired" }, this._cancellation);

        // Act
        ImmutableList<GameSnapshot> snapshots = await service.Snapshots(this._cancellation);

        // Assert
        Assert.Equal(
            [(id, TinyRules.RulesetId, First)],
            snapshots.Select(snapshot => (snapshot.Record.Id, snapshot.Rules.Id, snapshot.State.DecidingSeat!.Value))
        );
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
        string token = await Sit(this._service, id, First, this._cancellation);

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
        string token = await Sit(this._service, id, First, this._cancellation);

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
        string token = await Sit(this._service, id, First, this._cancellation);
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
        string token = await Sit(this._service, id, First, this._cancellation);
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
        string token = await Sit(this._service, id, Second, this._cancellation);

        // Act
        Task<GameSnapshot> submitting = this._service.Submit(id, token, Pass(Second), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourTurnException>(() => submitting);
    }

    [Fact]
    public async Task Submit_WhenAProposalToEndComesOutOfTurnFromASeatThatMay_TakesIt()
    {
        // Arrange - the first seat decides, but the second may propose ending the game
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await Sit(this._service, id, Second, this._cancellation);
        ActionDescriptor proposal = new ProposeEndAction(Second, Resigns: false).Describe();

        // Act
        GameSnapshot snapshot = await this._service.Submit(id, token, proposal, this._cancellation);

        // Assert
        Assert.Null(snapshot.State.DecidingSeat);
    }

    [Fact]
    public async Task Submit_WhenAProposalToEndComesFromASeatThatMayNot_ThrowsNotYourTurnException()
    {
        // Arrange - the first seat has passed, so the second decides; the first may not propose
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await Sit(this._service, id, First, this._cancellation);
        _ = await this._service.Submit(id, token, Pass(First), this._cancellation);
        ActionDescriptor proposal = new ProposeEndAction(First, Resigns: true).Describe();

        // Act
        Task<GameSnapshot> submitting = this._service.Submit(id, token, proposal, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourTurnException>(() => submitting);
    }

    [Fact]
    public async Task Submit_WhenASeatThatMayNotRejoinsOutOfTurn_ThrowsNotYourTurnException()
    {
        // Arrange - the first seat decides; the second never gave up its turns
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await Sit(this._service, id, Second, this._cancellation);
        ActionDescriptor rejoin = new RejoinAction(Second).Describe();

        // Act
        Task<GameSnapshot> submitting = this._service.Submit(id, token, rejoin, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourTurnException>(() => submitting);
    }

    [Fact]
    public async Task Submit_WhenTheGameIsOver_ThrowsNotYourTurnException()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);
        string token = await Sit(this._service, id, First, this._cancellation);
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
        string token = await Sit(this._service, id, First, this._cancellation);
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
    public async Task Get_WhenTheDecisionIsTimed_SaysHowLongIsLeft()
    {
        // Arrange
        (GameId id, _) = await this.SecondSeatToPass();
        this._time.Pass(FiveSeconds);

        // Act
        GameSnapshot snapshot = await this._paced.Get(id, this._cancellation);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(25), snapshot.TimeLeft);
    }

    [Fact]
    public async Task Get_WhenTheTimerIsSet_TakesItsLength()
    {
        // Arrange
        GameServiceSettings settings = GameServiceSettings.Default with
        {
            Timers = ImmutableDictionary<string, TimeSpan>.Empty.Add(TinyRules.Timer, FiveSeconds),
        };
        GameService service = this.Service(settings);
        GameId id = await service.Create(TinyRules.RulesetId, [Bot, SeatPlan.Open], this._cancellation);
        _ = await service.Claim(id, Second, this._cancellation);
        GameSnapshot started = await service.Advance(id, 1, this._cancellation);

        // Act
        GameSnapshot snapshot = await service.Get(id, this._cancellation);

        // Assert
        Assert.Equal((Second, FiveSeconds), (started.State.DecidingSeat, snapshot.TimeLeft));
    }

    [Fact]
    public async Task Advance_WhenATimedDecisionHasRunOutOfTime_DecidesWhatItsTimerDecides()
    {
        // Arrange
        (GameId id, GameSnapshot waiting) = await this.SecondSeatToPass();
        this._time.Pass(TimeSpan.FromSeconds(31));

        // Act
        GameSnapshot snapshot = await this._paced.Advance(id, waiting.Record.Version, this._cancellation);

        // Assert
        Assert.Equal(
            (First, "pass", "1", null),
            (
                snapshot.State.DecidingSeat,
                snapshot.Record.Actions[^1].Kind,
                snapshot.Record.Actions[^1].Arguments["actor"],
                snapshot.TimeLeft
            )
        );
    }

    [Fact]
    public async Task Advance_WhenATimedPersonStillHasTime_ChangesNothing()
    {
        // Arrange
        (GameId id, GameSnapshot waiting) = await this.SecondSeatToPass();
        this._time.Pass(FiveSeconds);

        // Act
        GameSnapshot snapshot = await this._paced.Advance(id, waiting.Record.Version, this._cancellation);

        // Assert
        Assert.Equal(waiting.Record.Version, snapshot.Record.Version);
    }

    [Fact]
    public async Task Get_WhenAPersonTakesLongOverWhatNoTimerLimits_OpensTheVoteOnWaitingOnlyAfterTheGrace()
    {
        // Arrange - the first seat, whose player is away, decides; a person plays the second
        (GameId id, _, _) = await this.FirstSeatAway();
        GameSnapshot before = await this._paced.Get(id, this._cancellation);
        this._time.Pass(Grace);

        // Act
        GameSnapshot after = await this._paced.Get(id, this._cancellation);

        // Assert
        Assert.Equal(
            (First, false, Grace, true, null),
            (before.Away!.Absent, before.Away.IsOpen, before.Away.GraceLeft, after.Away!.IsOpen, after.TimeLeft)
        );
        Assert.Equal([Second], after.Away.Voters);
    }

    [Fact]
    public async Task Advance_WhenEveryoneHasStoppedWaitingForASeatAway_PassesForIt()
    {
        // Arrange
        (GameId id, string voter, _) = await this.FirstSeatAway();
        this._time.Pass(Grace);
        GameSnapshot voted = await this._paced.Submit(id, voter, StopWaitingFor(First), this._cancellation);

        // Act
        GameSnapshot snapshot = await this._paced.Advance(id, voted.Record.Version, this._cancellation);

        // Assert
        Assert.Equal(
            (TimeSpan.Zero, true, Second, PassAwayAction.Kind),
            (voted.TimeLeft, voted.Away!.IsAgreed, snapshot.State.DecidingSeat, snapshot.Record.Actions[^1].Kind)
        );
    }

    [Fact]
    public async Task Get_WhenTheSeatAwayMissedItsLastTurn_PassesItAtOnceAsTheVotesStand()
    {
        // Arrange - the first seat was passed while away; the second has passed back to it
        (GameId id, string voter, _) = await this.FirstSeatPassedAway();

        // Act
        GameSnapshot snapshot = await this._paced.Get(id, this._cancellation);

        // Assert
        Assert.Equal(
            (First, true, TimeSpan.Zero),
            (snapshot.State.DecidingSeat, snapshot.Away!.IsOpen, snapshot.TimeLeft)
        );
        Assert.NotNull(voter);
    }

    [Fact]
    public async Task Get_WhenTheSeatAwaySaysItIsBack_GivesItItsGraceAgain()
    {
        // Arrange
        (GameId id, _, string token) = await this.FirstSeatPassedAway();
        ActionDescriptor back = new RejoinAction(First).Describe();

        // Act
        GameSnapshot snapshot = await this._paced.Submit(id, token, back, this._cancellation);

        // Assert
        Assert.Equal((false, Grace), (snapshot.Away!.IsOpen, snapshot.TimeLeft));
    }

    [Fact]
    public async Task Submit_WhenVotingToKeepWaitingBeforeTheVoteIsOpen_TakesIt()
    {
        // Arrange
        (GameId id, string voter, _) = await this.FirstSeatAway();
        ActionDescriptor wait = new VoteAwayAction(Second, First, Stops: false).Describe();

        // Act
        GameSnapshot snapshot = await this._paced.Submit(id, voter, wait, this._cancellation);

        // Assert
        Assert.Equal((1, false), (snapshot.Record.Actions.Count, snapshot.Away!.IsOpen));
    }

    [Theory]
    [InlineData("before the grace")]
    [InlineData("for a seat not deciding")]
    [InlineData("while no one may vote")]
    [InlineData("for oneself")]
    public async Task Submit_WhenAVoteOnWaitingMayNotBeCast_ThrowsNotYourTurnException(string when)
    {
        // Arrange
        (GameId id, string voter, string first) = await this.FirstSeatAway();
        this._time.Pass(when == "before the grace" ? TimeSpan.Zero : Grace);
        _ = when == "while no one may vote"
            ? await this._paced.Submit(id, first, Pass(First), this._cancellation)
            : null;
        (string token, ActionDescriptor vote) = when switch
        {
            "while no one may vote" => (first, StopWaitingFor(Second, First)),
            "for oneself" => (voter, new VoteAwayAction(Second, Second, Stops: false).Describe()),
            "for a seat not deciding" => (voter, StopWaitingFor(Second, Second)),
            _ => (voter, StopWaitingFor(First)),
        };

        // Act
        Task<GameSnapshot> submitting = this._paced.Submit(id, token, vote, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourTurnException>(() => submitting);
    }

    [Fact]
    public async Task Submit_WhenAPlayerPassesAsIfAway_ThrowsIllegalActionException()
    {
        // Arrange
        (GameId id, _, string token) = await this.FirstSeatAway();
        ActionDescriptor pass = new PassAwayAction(First).Describe();

        // Act
        Task<GameSnapshot> submitting = this._paced.Submit(id, token, pass, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<IllegalActionException>(() => submitting);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_WhenABotDecidesOrNoPersonCouldVote_HasNoVoteOnWaiting(bool isBotDeciding)
    {
        // Arrange
        GameId id = await this._paced.Create(
            TinyRules.RulesetId,
            [isBotDeciding ? Bot : SeatPlan.Open, SeatPlan.Open],
            this._cancellation
        );
        _ = isBotDeciding ? await this._paced.Claim(id, Second, this._cancellation) : null;

        // Act
        GameSnapshot snapshot = await this._paced.Get(id, this._cancellation);

        // Assert
        Assert.Null(snapshot.Away);
    }

    [Fact]
    public async Task Get_WhenThePlayerHasRolled_TellsOfTheRoll()
    {
        // Arrange
        GameId id =
            await this._paced.Create(RollingRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await Sit(this._paced, id, First, this._cancellation);
        _ = await this._paced.Submit(id, token, new RollAction(First).Describe(), this._cancellation);

        // Act
        GameSnapshot snapshot = await this._paced.Get(id, this._cancellation);

        // Assert
        Assert.Equal([new DiceRolled(First, [4])], snapshot.Events);
    }

    private static ActionDescriptor Pass(SeatId seat) => new PassAction(seat).Describe();

    /// <summary>
    /// Claims <paramref name="seat"/>, then every seat still open, for no one in particular, so that the game
    /// begins; the token holding <paramref name="seat"/>.
    /// </summary>
    private static async Task<string> Sit(
        GameService service,
        GameId id,
        SeatId seat,
        CancellationToken cancellationToken
    )
    {
        string token = await service.Claim(id, seat, cancellationToken);
        GameSnapshot snapshot = await service.Get(id, cancellationToken);
        ImmutableList<SeatId> open =
        [
            .. snapshot.Record.Controllers
                .Select((controller, other) => (controller, other))
                .Where(held => held.controller.IsOpen)
                .Select(held => new SeatId(held.other)),
        ];
        foreach (SeatId other in open)
        {
            _ = await service.Claim(id, other, cancellationToken);
        }

        return token;
    }

    private static ActionDescriptor StopWaitingFor(SeatId absent, SeatId? voter = null) =>
        new VoteAwayAction(voter ?? Second, absent, Stops: true).Describe();

    /// <summary>
    /// A paced game of two people in which the first seat, whose player is away, decides; the second player has
    /// seen the game, so the first seat's grace has begun. The tokens of both: the voter's and the one away.
    /// </summary>
    private async Task<(GameId Id, string Voter, string Away)> FirstSeatAway()
    {
        GameId id =
            await this._paced.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string away = await this._paced.Claim(id, First, this._cancellation);
        string voter = await this._paced.Claim(id, Second, this._cancellation);
        _ = await this._paced.Get(id, this._cancellation);
        return (id, voter, away);
    }

    /// <summary>
    /// As <see cref="FirstSeatAway"/>, once the second seat stopped waiting for the first, which passed, and
    /// the second passed back to it.
    /// </summary>
    private async Task<(GameId Id, string Voter, string Away)> FirstSeatPassedAway()
    {
        (GameId id, string voter, string away) = await this.FirstSeatAway();
        this._time.Pass(Grace);
        GameSnapshot voted = await this._paced.Submit(id, voter, StopWaitingFor(First), this._cancellation);
        _ = await this._paced.Advance(id, voted.Record.Version, this._cancellation);
        _ = await this._paced.Submit(id, voter, Pass(Second), this._cancellation);
        return (id, voter, away);
    }

    /// <summary>A paced game of two people in which the first has passed: the second has 30 seconds to.</summary>
    private async Task<(GameId Id, GameSnapshot Waiting)> SecondSeatToPass()
    {
        GameId id =
            await this._paced.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await Sit(this._paced, id, First, this._cancellation);
        GameSnapshot waiting = await this._paced.Submit(id, token, Pass(First), this._cancellation);
        return (id, waiting);
    }

    private GameService Service(GameServiceSettings settings, InMemoryGameStore? store = null) =>
        new(
            new RulesetCatalog([new TinyRules(GameLength), new RollingRules(GameLength)]),
            new BotCatalog([new FirstChoiceBot()]),
            new ScenarioCatalog(
                [
                    new Scenario(TinyRules.RulesetId, "Second first", "The second seat starts.")
                    {
                        Id = SecondFirst,
                        Turn = "Seat 1",
                    },
                    new Scenario(RollingRules.RulesetId, "Rolling", "Another ruleset's.") { Id = Rolling },
                ]
            ),
            store ?? new InMemoryGameStore(),
            this._notifier,
            new LoadedDice(4),
            new DecisionClock(this._time),
            settings
        );
}
