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

    private static readonly ActionDescriptor FaceOff =
        new FaceOffAction(First, new CellId(1), new CellId(3)).Describe();

    private static readonly TimeSpan FiveSeconds = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Grace = GameServiceSettings.DefaultAwayGrace;

    private readonly RecordingNotifier _notifier = new();
    private readonly SteppedTime _time = new();
    private readonly InMemoryPlayerStore _players = new();
    private readonly GameGate _gate = new();
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
    public async Task Create_WhenTheRulesetDealsEachGame_DealsItBySeedAndReplaysFromTheDeal()
    {
        // Arrange - the loaded dice roll the seed, 4, which the tiny rules score to the first seat
        GameService service = this.Service(GameServiceSettings.Default, isDealt: true);
        GameId id = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);

        // Act
        GameSnapshot snapshot = await service.Get(id, this._cancellation);

        // Assert
        Assert.Equal(
            (ScenarioStart.DealKind, 4),
            (snapshot.Record.Actions[0].Kind, snapshot.State.Scores[First])
        );
    }

    [Fact]
    public async Task Create_WhenTheArmiesAreAskedForAlike_DealsThemAlike()
    {
        // Arrange - the tiny rules score 1 to the second seat for a game dealt alike
        GameService service = this.Service(GameServiceSettings.Default, isDealt: true);
        GameId id = await service.Create(
            TinyRules.RulesetId,
            [SeatPlan.Open, SeatPlan.Open],
            null,
            new Dealing(AreAlike: true),
            this._cancellation
        );

        // Act
        GameSnapshot snapshot = await service.Get(id, this._cancellation);

        // Assert
        Assert.Equal(
            (true, 1),
            (ScenarioStart.AreAlike(snapshot.Record.Actions), snapshot.State.Scores[Second])
        );
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Create_WhenTheArmiesAreAskedToBeDrafted_DraftsThemIfTheRulesLetPlayers(
        bool isDraftable,
        bool isDrafted
    )
    {
        // Arrange
        GameService service = this.Service(GameServiceSettings.Default, isDealt: true, isDraftable: isDraftable);
        GameId id = await service.Create(
            TinyRules.RulesetId,
            [SeatPlan.Open, SeatPlan.Open],
            null,
            new Dealing { IsDrafted = true },
            this._cancellation
        );

        // Act
        GameSnapshot snapshot = await service.Get(id, this._cancellation);

        // Assert
        Assert.Equal(
            (isDrafted, isDrafted),
            (ScenarioStart.IsDrafted(snapshot.Record.Actions), snapshot.State.Draft is not null)
        );
    }

    [Fact]
    public async Task Create_WhenTheArmiesOfAnotherGameAreAskedFor_DealsAsItWasDealt()
    {
        // Arrange - a game dealt alike, played again with its armies though not asked for alike
        GameService service = this.Service(GameServiceSettings.Default, isDealt: true);
        ImmutableList<SeatPlan> seats = [SeatPlan.Open, SeatPlan.Open];
        GameId first =
            await service.Create(TinyRules.RulesetId, seats, null, new Dealing(AreAlike: true), this._cancellation);
        GameId again =
            await service.Create(TinyRules.RulesetId, seats, null, new Dealing(ArmiesOf: first), this._cancellation);

        // Act
        GameSnapshot snapshot = await service.Get(again, this._cancellation);

        // Assert
        GameSnapshot original = await service.Get(first, this._cancellation);
        Assert.Equal(original.Record.Actions[0], snapshot.Record.Actions[0]);
    }

    [Fact]
    public async Task Create_WhenTheArmiesOfAGameNotDealtOrUnknownAreAskedFor_DealsAnew()
    {
        // Arrange - a rolling game, not dealt, and a game that never was
        GameService service = this.Service(GameServiceSettings.Default, isDealt: true);
        ImmutableList<SeatPlan> seats = [SeatPlan.Open, SeatPlan.Open];
        GameId rolling = await service.Create(RollingRules.RulesetId, seats, this._cancellation);

        // Act
        ImmutableList<GameId> anew =
        [
            await service.Create(TinyRules.RulesetId, seats, null, new Dealing(ArmiesOf: rolling), this._cancellation),
            await service.Create(
                TinyRules.RulesetId,
                seats,
                null,
                new Dealing(ArmiesOf: GameId.New()),
                this._cancellation
            ),
        ];

        // Assert
        foreach (GameId id in anew)
        {
            GameSnapshot snapshot = await service.Get(id, this._cancellation);
            Assert.Equal((ScenarioStart.DealKind, 0), (snapshot.Record.Actions[0].Kind, snapshot.State.Scores[Second]));
        }
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
        Assert.Equal<(PlayerId?, string?, bool)>((player, null, false), (seat.Claimant, seat.Nickname, seat.IsAi));
    }

    [Fact]
    public async Task Claim_WhenAGuestClaimsASeatForPlayers_ThrowsSeatForPlayersException()
    {
        // Arrange
        SeatPlan forPlayers = SeatPlan.Open with { IsForPlayers = true };
        GameId id = await this._service.Create(TinyRules.RulesetId, [forPlayers, Bot], this._cancellation);

        // Act
        Task<string> claiming = this._service.Claim(id, First, null, "Guest", this._cancellation);

        // Assert
        SeatForPlayersException refused = await Assert.ThrowsAsync<SeatForPlayersException>(() => claiming);
        Assert.Equal(GameRequestFailure.Forbidden, refused.Failure);
    }

    [Fact]
    public async Task Claim_WhenAPlayerSignedInClaimsASeatForPlayers_SeatsThemAtASeatStillForPlayers()
    {
        // Arrange
        SeatPlan forPlayers = SeatPlan.Open with { IsForPlayers = true };
        GameId id = await this._service.Create(TinyRules.RulesetId, [forPlayers, Bot], this._cancellation);
        PlayerId player = new("ada");

        // Act
        _ = await this._service.Claim(id, First, player, null, this._cancellation);

        // Assert
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);
        HumanSeat seat = (HumanSeat)snapshot.Record.Controllers[0];
        Assert.Equal<(PlayerId?, bool)>((player, true), (seat.Claimant, seat.IsForPlayers));
    }

    [Fact]
    public async Task Claim_WhenAnAiClaims_MarksTheSeatAsAnAis()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);

        // Act
        _ = await this._service.Claim(id, First, null, "Hal", true, this._cancellation);

        // Assert
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);
        HumanSeat seat = (HumanSeat)snapshot.Record.Controllers[0];
        Assert.Equal(("Hal", true), (seat.Nickname, seat.IsAi));
    }

    [Theory]
    [InlineData("ada", "Impostor", false, "ada", null)]
    [InlineData(null, "  Hal  ", false, null, "Hal")]
    [InlineData("ada", " Sage ", true, "ada", "Sage")]
    [InlineData(null, "Hal", true, null, "Hal")]
    public async Task Identify_WhenTheSeatsHolderSaysWhoTheyAreNow_KnowsThemSoAndKeepsTheRest(
        string? player,
        string? nickname,
        bool isAi,
        string? claimant,
        string? kept
    )
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);
        string token = await this._service.Claim(id, First, null, null, isAi, this._cancellation);
        PlayerId? who = player is null ? null : new PlayerId(player);

        // Act
        await this._service.Identify(id, First, token, who, nickname, this._cancellation);

        // Assert
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);
        HumanSeat seat = (HumanSeat)snapshot.Record.Controllers[0];
        Assert.Equal(
            (claimant, kept, isAi, true),
            (seat.Claimant?.Value, seat.Nickname, seat.IsAi, seat.IsHeldBy(token))
        );
    }

    [Fact]
    public async Task Claim_WhenOnePlayersAisClaimSeveralSeats_SeatsEachByItsOwnNickname()
    {
        // Arrange
        GameId id = await this._service.Create(
            TinyRules.RulesetId,
            [SeatPlan.Open, SeatPlan.Open],
            this._cancellation
        );
        PlayerId player = new("ada");

        // Act
        string sage = await this._service.Claim(id, First, player, "Sage", true, this._cancellation);
        string hal = await this._service.Claim(id, new SeatId(1), player, "Hal", true, this._cancellation);

        // Assert
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);
        HumanSeat first = (HumanSeat)snapshot.Record.Controllers[0];
        HumanSeat second = (HumanSeat)snapshot.Record.Controllers[1];
        Assert.Equal(
            ("ada Sage", true, "ada Hal", true),
            (
                $"{first.Claimant?.Value} {first.Nickname}",
                first.IsHeldBy(sage),
                $"{second.Claimant?.Value} {second.Nickname}",
                second.IsHeldBy(hal)
            )
        );
    }

    [Fact]
    public async Task Get_WhenAGameWaitsForItsSeats_SaysTheFirstToArriveMayFillThemAfterTheWait()
    {
        // Arrange
        GameId id = await this.CreateOpen();
        _ = await this._service.Claim(id, Second, this._cancellation);
        _ = await this._service.Get(id, this._cancellation);
        this._time.Pass(FiveSeconds);

        // Act
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);

        // Assert
        TimeSpan wait = GameServiceSettings.DefaultFillWait;
        Assert.Equal(new Vacancy(Second, wait - FiveSeconds, (wait * 2) - FiveSeconds), snapshot.Vacancy);
    }

    [Fact]
    public async Task Get_WhenNoOneHasTakenASeat_NamesNoOneToFillThemFirst()
    {
        // Arrange
        GameId id = await this.CreateOpen();

        // Act
        GameSnapshot snapshot = await this._service.Get(id, this._cancellation);

        // Assert
        Assert.Null(snapshot.Vacancy?.FirstArrival);
    }

    [Fact]
    public async Task FillOpenSeats_WhenTheFirstToArriveHasWaited_SeatsABotAndTheGameBegins()
    {
        // Arrange
        GameId id = await this.CreateOpen();
        string token = await this._service.Claim(id, First, this._cancellation);
        _ = await this._service.Get(id, this._cancellation);
        this._time.Pass(GameServiceSettings.DefaultFillWait);

        // Act
        GameSnapshot filled = await this._service.FillOpenSeats(id, token, this._cancellation);

        // Assert
        Assert.Equal<(SeatController, bool, Vacancy?, bool)>(
            (new BotSeat(FirstChoiceBot.BotId), false, null, false),
            (filled.Record.Controllers[1], filled.Record.IsWaiting, filled.Vacancy, filled.Record.HasChangedHands)
        );
    }

    [Fact]
    public async Task FillOpenSeats_WhenNoBotIsLeftToSeat_LeavesTheSeatOpen()
    {
        // Arrange
        GameService service = this.Service(GameServiceSettings.Default, bots: []);
        GameId id = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await service.Claim(id, First, this._cancellation);
        _ = await service.Get(id, this._cancellation);
        this._time.Pass(GameServiceSettings.DefaultFillWait);

        // Act
        GameSnapshot filled = await service.FillOpenSeats(id, token, this._cancellation);

        // Assert
        Assert.True(filled.Record.Controllers[1].IsOpen);
    }

    [Fact]
    public async Task FillOpenSeats_WhenTooSoon_Refuses()
    {
        // Arrange
        GameId id = await this.CreateOpen();
        string token = await this._service.Claim(id, First, this._cancellation);

        // Act
        Task<GameSnapshot> filling = this._service.FillOpenSeats(id, token, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<TooSoonToFillException>(() => filling);
    }

    [Fact]
    public async Task FillOpenSeats_WhenTheTokenHoldsNoSeat_Refuses()
    {
        // Arrange
        GameId id = await this.CreateOpen();

        // Act
        Task<GameSnapshot> filling = this._service.FillOpenSeats(id, "no-ones", this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotAPlayerException>(() => filling);
    }

    [Fact]
    public async Task FillOpenSeats_WhenNoSeatIsOpen_LeavesTheGameAsItIs()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);
        string token = await this._service.Claim(id, First, this._cancellation);
        GameSnapshot before = await this._service.Get(id, this._cancellation);

        // Act
        GameSnapshot after = await this._service.FillOpenSeats(id, token, this._cancellation);

        // Assert
        Assert.Equal(before.Record.Version, after.Record.Version);
    }

    [Fact]
    public async Task TakeBack_WhenThePlayerClaimedTheSeat_HandsThemATokenForItAndTheOldOneNoMore()
    {
        // Arrange
        PlayerId ada = new("ada");
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);
        string old = await this._service.Claim(id, First, ada, null, this._cancellation);

        // Act
        string token = await this._service.TakeBack(id, First, ada, this._cancellation);

        // Assert
        HumanSeat seat = (HumanSeat)(await this._service.Get(id, this._cancellation)).Record.Controllers[0];
        Assert.Equal((true, false, ada), (seat.IsHeldBy(token), seat.IsHeldBy(old), seat.Claimant));
    }

    [Fact]
    public async Task Get_WhenItsRulesNoLongerReplayTheGame_ThrowsGameUnreplayableException()
    {
        // Arrange - a game whose first action, the second seat passing, is not one its rules allow
        InMemoryGameStore store = new();
        GameService service = this.Service(GameServiceSettings.Default, store);
        GameId id = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        GameRecord kept = (await store.Find(id, this._cancellation))!;
        await store.Remove([id], this._cancellation);
        await store.Add(kept with { Actions = [new PassAction(Second).Describe()] }, this._cancellation);

        // Act
        Task<GameSnapshot> getting = service.Get(id, this._cancellation);

        // Assert
        GameUnreplayableException refused = await Assert.ThrowsAsync<GameUnreplayableException>(() => getting);
        Assert.Equal(GameRequestFailure.Conflict, refused.Failure);
    }

    [Fact]
    public async Task Awaiting_WhenGamesWaitForThePlayerOrAnother_ListsThoseWaitingForThemEarliestChangedFirst()
    {
        // Arrange - Ada plays first at both games, Bob after Ada; the game created last is the one changed earliest
        PlayerId ada = new("ada");
        PlayerId bob = new("bob");
        GameId later = await this.CreateOpen();
        this._time.Pass(FiveSeconds);
        GameId earlier = await this.CreateOpen();
        foreach (GameId id in new[] { earlier, later })
        {
            _ = await this._service.Claim(id, First, ada, null, this._cancellation);
            _ = await this._service.Claim(id, Second, bob, null, this._cancellation);
            this._time.Pass(FiveSeconds);
        }

        // Act
        ImmutableList<AwaitedGame> awaited = await this._service.Awaiting(ada, this._cancellation);

        // Assert
        Assert.Equal(
            ($"{earlier},{later}", $"{First},{First}", 0),
            (
                string.Join(",", awaited.Select(game => game.Record.Id)),
                string.Join(",", awaited.Select(game => game.Seat)),
                (await this._service.Awaiting(bob, this._cancellation)).Count
            )
        );
    }

    [Theory]
    [InlineData(true, TinyRules.RulesetId)]
    [InlineData(false, "retired")]
    public async Task Awaiting_WhenTheSeatIsTheirAisOrTheRulesetIsGone_ListsNothing(bool isAi, string ruleset)
    {
        // Arrange
        PlayerId ada = new("ada");
        InMemoryGameStore store = new();
        GameService service = this.Service(GameServiceSettings.Default, store);
        GameId id = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        _ = await service.Claim(id, First, ada, null, isAi, this._cancellation);
        _ = await service.Claim(id, Second, this._cancellation);
        GameRecord kept = (await store.Find(id, this._cancellation))!;
        await store.Remove([id], this._cancellation);
        await store.Add(kept with { RulesetId = ruleset }, this._cancellation);

        // Act
        ImmutableList<AwaitedGame> awaited = await service.Awaiting(ada, this._cancellation);

        // Assert
        Assert.Empty(awaited);
    }

    [Theory]
    [InlineData("someone else's")]
    [InlineData("a guest's")]
    [InlineData("their AI's")]
    [InlineData("a bot's")]
    [InlineData("no seat")]
    public async Task TakeBack_WhenTheSeatIsNotOneThePlayerPlays_Refuses(string whose)
    {
        // Arrange
        PlayerId ada = new("ada");
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);
        _ = whose switch
        {
            "someone else's" => await this._service.Claim(id, First, new PlayerId("bob"), null, this._cancellation),
            "a guest's" => await this._service.Claim(id, First, this._cancellation),
            _ => await this._service.Claim(id, First, ada, null, true, this._cancellation),
        };
        SeatId seat = whose switch
        {
            "a bot's" => Second,
            "no seat" => new SeatId(7),
            _ => First,
        };

        // Act
        Task<string> taking = this._service.TakeBack(id, seat, ada, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourSeatException>(() => taking);
    }

    [Fact]
    public async Task TakeOver_WhenABotPlaysTheSeat_HandsItToThePlayerForGood()
    {
        // Arrange - the game set up for players only, so that the seat taken over is too
        PlayerId ada = new("ada");
        SeatPlan forPlayers = SeatPlan.Open with { IsForPlayers = true };
        SeatPlan bot = Bot with { IsForPlayers = true };
        GameId id = await this._service.Create(TinyRules.RulesetId, [forPlayers, bot], this._cancellation);
        _ = await this._service.Claim(id, First, new PlayerId("bob"), null, this._cancellation);

        // Act
        string token = await this._service.TakeOver(id, Second, ada, null, this._cancellation);

        // Assert
        GameRecord record = (await this._service.Get(id, this._cancellation)).Record;
        HumanSeat seat = (HumanSeat)record.Controllers[1];
        Assert.Equal<(bool, PlayerId?, bool, bool, int, bool)>(
            (true, ada, false, true, record.Version, true),
            (seat.IsHeldBy(token), seat.Claimant, seat.IsAi, seat.IsForPlayers, seat.Arrival, record.HasChangedHands)
        );
    }

    [Fact]
    public async Task TakeOver_WhenAGuestTakesABotsSeatOpenToAnyone_SeatsThemByTheirNickname()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);

        // Act
        string token = await this._service.TakeOver(id, Second, null, "Gus", this._cancellation);

        // Assert
        HumanSeat seat = (HumanSeat)(await this._service.Get(id, this._cancellation)).Record.Controllers[1];
        Assert.Equal<(bool, PlayerId?, string?)>(
            (true, null, "Gus"),
            (seat.IsHeldBy(token), seat.Claimant, seat.Nickname)
        );
    }

    [Fact]
    public async Task TakeOver_WhenAGuestAsksForASeatOnlyForPlayers_ThrowsSeatForPlayersException()
    {
        // Arrange
        SeatPlan bot = Bot with { IsForPlayers = true };
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, bot], this._cancellation);

        // Act
        Task<string> taking = this._service.TakeOver(id, Second, null, "Gus", this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<SeatForPlayersException>(() => taking);
    }

    [Fact]
    public async Task TakeOver_WhenTheGameStillWaitsForItsOpenSeats_ChangesNoHands()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);

        // Act
        _ = await this._service.TakeOver(id, Second, new PlayerId("ada"), null, this._cancellation);

        // Assert
        Assert.False((await this._service.Get(id, this._cancellation)).Record.HasChangedHands);
    }

    [Theory]
    [InlineData("bob", true, true)]
    [InlineData("ada", true, false)]
    [InlineData("bob", false, false)]
    public async Task Identify_WhenTheGameHasBegun_ChangesHandsOnlyForAnotherPlayer(
        string who,
        bool hasBegun,
        bool expected
    )
    {
        // Arrange - the game begins once its first seat is taken, if a bot plays the second
        SeatPlan second = hasBegun ? Bot : SeatPlan.Open;
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, second], this._cancellation);
        string token = await this._service.Claim(id, First, new PlayerId("ada"), null, this._cancellation);

        // Act
        await this._service.Identify(id, First, token, new PlayerId(who), null, this._cancellation);

        // Assert
        Assert.Equal(expected, (await this._service.Get(id, this._cancellation)).Record.HasChangedHands);
    }

    [Theory]
    [InlineData("a person's")]
    [InlineData("no seat")]
    [InlineData("seated already")]
    public async Task TakeOver_WhenNoBotPlaysTheSeatOrThePlayerSitsThere_Refuses(string why)
    {
        // Arrange
        PlayerId ada = new("ada");
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);
        _ = await this._service.Claim(id, First, why == "seated already" ? ada : null, null, this._cancellation);
        SeatId seat = why switch
        {
            "a person's" => First,
            "no seat" => new SeatId(7),
            _ => Second,
        };

        // Act
        Task<string> taking = this._service.TakeOver(id, seat, ada, null, this._cancellation);

        // Assert
        SeatNotTakenOverException refused = await Assert.ThrowsAsync<SeatNotTakenOverException>(() => taking);
        Assert.Equal(GameRequestFailure.Conflict, refused.Failure);
    }

    [Fact]
    public async Task TakeOver_WhenTheGameIsOver_Refuses()
    {
        // Arrange - a game the second seat ended, its seat then handed to a bot
        InMemoryGameStore store = new();
        GameService service = this.Service(GameServiceSettings.Default, store);
        GameId id = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await Sit(service, id, Second, this._cancellation);
        _ = await service.Submit(id, token, new ProposeEndAction(Second, false).Describe(), this._cancellation);
        GameRecord ended = (await store.Find(id, this._cancellation))!;
        await store.Remove([id], this._cancellation);
        await store.Add(
            ended with { Controllers = ended.Controllers.SetItem(1, new BotSeat(FirstChoiceBot.BotId)) },
            this._cancellation
        );

        // Act
        Task<string> taking = service.TakeOver(id, Second, new PlayerId("ada"), null, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<SeatNotTakenOverException>(() => taking);
    }

    [Fact]
    public async Task Identify_WhenTheTokenDoesNotHoldTheSeat_Refuses()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);
        _ = await this._service.Claim(id, First, this._cancellation);

        // Act
        Task identifying = this._service.Identify(id, Second, "not-a-seat", null, "Hal", this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourSeatException>(() => identifying);
    }

    [Fact]
    public async Task Identify_WhenTheTokenHoldsAnotherSeat_Refuses()
    {
        // Arrange
        GameId id = await this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, Bot], this._cancellation);
        _ = await this._service.Claim(id, First, this._cancellation);

        // Act
        Task identifying = this._service.Identify(id, First, "not-its-token", null, "Hal", this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourSeatException>(() => identifying);
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
        GameId id = await this.CreateOpen();
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
    public async Task Snapshots_WhenAGameNoLongerReplays_ListsOnlyTheOthers()
    {
        // Arrange - a game whose actions its rules no longer allow, as after the rules change
        InMemoryGameStore store = new();
        GameService service = this.Service(GameServiceSettings.Default, store);
        GameId id = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        GameRecord kept = (await store.Find(id, this._cancellation))!;
        ActionDescriptor move = new MoveAction(First, new CellId(0), new CellId(1)).Describe();
        await store.Add(kept with { Id = new GameId("broken"), Actions = [move] }, this._cancellation);

        // Act
        ImmutableList<GameSnapshot> snapshots = await service.Snapshots(this._cancellation);

        // Assert
        Assert.Equal([id], snapshots.Select(snapshot => snapshot.Record.Id));
    }

    [Fact]
    public async Task Page_WhenGamesChanged_ListsTheOfferedOnesLastChangedFirst()
    {
        // Arrange
        InMemoryGameStore store = new();
        GameService service = this.Service(GameServiceSettings.Default, store);
        GameId older = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        this._time.Pass(FiveSeconds);
        GameId newer = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        this._time.Pass(FiveSeconds);
        _ = await Sit(service, older, First, this._cancellation);
        GameRecord kept = (await store.Find(newer, this._cancellation))!;
        await store.Add(kept with { Id = new GameId("retired"), RulesetId = "retired" }, this._cancellation);

        // Act
        GamePage page = await service.Page(new GameQuery(), this._cancellation);

        // Assert
        Assert.Equal([older, newer], page.Games.Select(game => game.Id));
    }

    [Fact]
    public async Task Page_WhenAskedForOneRuleset_ListsOnlyItsGames()
    {
        // Arrange
        GameId tiny = await this.CreateOpen();
        _ = await this._service.Create(RollingRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);

        // Act
        GamePage page = await this._service.Page(new GameQuery(TinyRules.RulesetId), this._cancellation);

        // Assert
        Assert.Equal(tiny, Assert.Single(page.Games).Id);
    }

    [Fact]
    public async Task Page_WhenAskedForOneGame_ListsItsGamesAtAnySize()
    {
        // Arrange
        _ = await this.CreateOpen();
        GameId rolling =
            await this._service.Create(RollingRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);

        // Act
        GamePage page = await this._service.Page(new GameQuery(GameId: RollingRules.RulesetId), this._cancellation);

        // Assert
        Assert.Equal(rolling, Assert.Single(page.Games).Id);
    }

    [Theory]
    [InlineData(false, TinyRules.RulesetId)]
    [InlineData(true, RollingRules.RulesetId)]
    public async Task Page_WhenAskedForPreviewsOrNot_ListsOnlyThoseOrOnlyOthers(bool preview, string expected)
    {
        // Arrange
        _ = await this.CreateOpen();
        _ = await this._service.Create(RollingRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);

        // Act
        GamePage page = await this._service.Page(new GameQuery(IsPreview: preview), this._cancellation);

        // Assert
        Assert.Equal(expected, Assert.Single(page.Games).RulesetId);
    }

    [Fact]
    public async Task Page_WhenAskedForTooBigAPage_ListsAPageOfTheMost()
    {
        // Arrange
        _ = await this.CreateOpen();

        // Act
        GamePage page = await this._service.Page(new GameQuery(Page: 0, PageSize: 1000), this._cancellation);

        // Assert
        Assert.Equal(1, page.Total);
    }

    [Fact]
    public async Task All_WhenMoreGamesMatchThanAPageHolds_ListsEachOfTheRulesetAskedFor()
    {
        // Arrange
        ImmutableList<GameId> tiny = [.. await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => this.CreateOpen()))];
        _ = await this._service.Create(RollingRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);

        // Act
        ImmutableList<GameListing> all =
            await this._service.All(new GameQuery(TinyRules.RulesetId, Page: 2, PageSize: 1), this._cancellation);

        // Assert
        Assert.Equal(tiny.Select(id => id.Value).Order(), all.Select(game => game.Id.Value).Order());
    }

    [Fact]
    public async Task PurgeAbandoned_WhenAGuestsGameWasLeftLongEnough_LetsGoOfItAlone()
    {
        // Arrange - a guest's game, a signed-in player's and nobody's, left long enough; and a guest's game since
        InMemoryGameStore store = new();
        GameService service = this.Service(GameServiceSettings.Default, store);
        GameId guest = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        _ = await service.Claim(guest, First, this._cancellation);
        GameId player = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        _ = await service.Claim(player, First, new PlayerId("ada"), null, this._cancellation);
        GameId nobody = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        this._time.Pass(GameServiceSettings.DefaultGuestGamesKept + FiveSeconds);
        GameId recent = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        _ = await service.Claim(recent, First, this._cancellation);

        // Act
        int purged = await service.PurgeAbandoned(this._cancellation);

        // Assert
        ImmutableList<GameRecord> kept = await store.List(this._cancellation);
        IEnumerable<string> expected = new[] { player, nobody, recent }.Select(id => id.Value);
        Assert.Equal(
            (1, string.Join(",", expected.Order(StringComparer.Ordinal))),
            (purged, string.Join(",", kept.Select(game => game.Id.Value)))
        );
    }

    [Fact]
    public async Task PurgeAbandoned_WhenGamesWaitLongForPlayersGoneAsLong_LetsGoOfThoseAlone()
    {
        // Arrange - each game but the last left long enough, the first seat to decide in each
        InMemoryGameStore store = new();
        GameService service = this.Service(GameServiceSettings.Default, store);
        DateTimeOffset now = this._time.Now;
        DateTimeOffset longAgo = now - GameServiceSettings.DefaultAwaitedGamesKept - FiveSeconds;
        await this._players.MarkSignedIn(new PlayerId("gone"), longAgo, this._cancellation);
        await this._players.MarkSignedIn(new PlayerId("back"), now, this._cancellation);
        GameRecord[] games =
        [
            Waiting("gone", "gone", longAgo),
            Waiting("back", "back", longAgo),
            Waiting("unknown", "unknown", longAgo),
            Waiting("guest", "gone", longAgo) with { Controllers = [new HumanSeat("hash"), new OpenSeat()] },
            Waiting("retired", "gone", longAgo) with { RulesetId = "retired" },
            Waiting("broken", "gone", longAgo) with { Actions = [new PassAction(Second).Describe()] },
            Waiting("bot", "gone", longAgo) with { Controllers = [new BotSeat(FirstChoiceBot.BotId), new OpenSeat()] },
            Waiting("recent", "gone", now),
        ];
        foreach (GameRecord game in games)
        {
            await store.Add(game, this._cancellation);
        }

        // Act
        int purged = await service.PurgeAbandoned(this._cancellation);

        // Assert
        ImmutableList<GameRecord> kept = await store.List(this._cancellation);
        Assert.Equal(
            (2, "back,bot,broken,recent,retired,unknown"),
            (purged, string.Join(",", kept.Select(game => game.Id.Value)))
        );
    }

    [Fact]
    public async Task Create_WhenCalled_SavesWhenTheGameWasCreated()
    {
        // Arrange
        InMemoryGameStore store = new();
        GameService service = this.Service(GameServiceSettings.Default, store);

        // Act
        GameId id = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);

        // Assert
        GameRecord? saved = await store.Find(id, this._cancellation);
        Assert.Equal((this._time.Now, this._time.Now), (saved?.Created, saved?.LastChanged));
    }

    [Fact]
    public async Task Submit_WhenItEndsTheGame_SavesItAsOverAndWhen()
    {
        // Arrange
        InMemoryGameStore store = new();
        GameService service = this.Service(GameServiceSettings.Default, store);
        GameId id = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await Sit(service, id, Second, this._cancellation);
        bool? wasOver = (await store.Find(id, this._cancellation))?.IsOver;
        this._time.Pass(FiveSeconds);

        // Act
        _ = await service.Submit(id, token, new ProposeEndAction(Second, false).Describe(), this._cancellation);

        // Assert
        GameRecord? saved = await store.Find(id, this._cancellation);
        Assert.Equal((false, true, this._time.Now), (wasOver, saved?.IsOver, saved?.LastChanged));
    }

    [Fact]
    public async Task Summarize_WhenGamesWereSavedBeforeItWasKeptWhetherTheyAreOver_WorksItOut()
    {
        // Arrange
        InMemoryGameStore store = new();
        GameService service = this.Service(GameServiceSettings.Default, store);
        GameId going = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        GameId over = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string token = await Sit(service, over, Second, this._cancellation);
        _ = await service.Submit(over, token, new ProposeEndAction(Second, false).Describe(), this._cancellation);
        GameRecord kept = (await store.Find(going, this._cancellation))!;
        ActionDescriptor move = new MoveAction(First, new CellId(0), new CellId(1)).Describe();
        GameRecord[] unsummarized =
        [
            kept with { Id = new GameId("going"), IsOver = null },
            (await store.Find(over, this._cancellation))! with { Id = new GameId("over"), IsOver = null },
            kept with { Id = new GameId("retired"), RulesetId = "retired", IsOver = null },
            kept with { Id = new GameId("broken"), Actions = [move], IsOver = null },
        ];
        foreach (GameRecord record in unsummarized)
        {
            await store.Add(record, this._cancellation);
        }

        // Act
        await service.Summarize(this._cancellation);

        // Assert
        bool?[] summaries =
        [
            .. await Task.WhenAll(
                unsummarized.Select(async record => (await store.Find(record.Id, this._cancellation))?.IsOver)
            ),
        ];
        Assert.Equal([false, true, true, true], summaries);
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
        GameId id = await this.CreateOpen();

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
        GameId id = await this.CreateOpen();

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
        GameId id = await this.CreateOpen();

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
        GameId id = await this.CreateOpen();
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
        GameId id = await this.CreateOpen();
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
        GameId id = await this.CreateOpen();
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
        GameId id = await this.CreateOpen();
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
        GameId id = await this.CreateOpen();
        string token = await Sit(this._service, id, First, this._cancellation);
        _ = await this._service.Submit(id, token, Pass(First), this._cancellation);
        ActionDescriptor proposal = new ProposeEndAction(First, Resigns: true).Describe();

        // Act
        Task<GameSnapshot> submitting = this._service.Submit(id, token, proposal, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourTurnException>(() => submitting);
    }

    [Fact]
    public async Task Submit_WhenAFaceOffComesOutOfTurnFromASeatWithThatChance_TakesIt()
    {
        // Arrange - the first seat has passed, so the second decides; the first's King may call a face-off
        GameService service = this.Service(GameServiceSettings.Default, hasFaceOff: true);
        (GameId id, string token) = await FirstPassed(service, this._cancellation);

        // Act
        GameSnapshot snapshot = await service.Submit(id, token, FaceOff, this._cancellation);

        // Assert
        Assert.Null(snapshot.State.Position.PieceAt(new CellId(1)));
    }

    [Fact]
    public async Task Submit_WhenAFaceOffComesOutOfTurnFromASeatWithoutThatChance_ThrowsNotYourTurnException()
    {
        // Arrange - the first seat has passed, so the second decides; the rules offer no face-off
        (GameId id, string token) = await FirstPassed(this._service, this._cancellation);

        // Act
        Task<GameSnapshot> submitting = this._service.Submit(id, token, FaceOff, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotYourTurnException>(() => submitting);
    }

    [Fact]
    public async Task Submit_WhenASeatThatMayNotRejoinsOutOfTurn_ThrowsNotYourTurnException()
    {
        // Arrange - the first seat decides; the second never gave up its turns
        GameId id = await this.CreateOpen();
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
        GameId id = await this.CreateOpen();
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
    public async Task Advance_WhenAnotherRequestIsChangingTheGame_WaitsForItToBeDone()
    {
        // Arrange
        GameId id = await this._paced.Create(TinyRules.RulesetId, [Bot, Bot], this._cancellation);
        IDisposable other = await this._gate.Enter(id);

        // Act
        Task<GameSnapshot> advancing = this._paced.Advance(id, 0, this._cancellation);
        bool waited = !advancing.IsCompleted;
        other.Dispose();
        GameSnapshot snapshot = await advancing;

        // Assert
        Assert.Equal((true, 1), (waited, snapshot.Record.Version));
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

    [Fact]
    public async Task ReplaceAway_WhenTheFirstToArriveWaitsOnAnotherPastTheGrace_SeatsABotThereAtOnce()
    {
        // Arrange - the second seat arrived first; the first, deciding, is away
        GameId id =
            await this._paced.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string first = await this._paced.Claim(id, Second, this._cancellation);
        _ = await this._paced.Claim(id, First, this._cancellation);
        _ = await this._paced.Get(id, this._cancellation);
        this._time.Pass(Grace);

        // Act
        GameSnapshot replaced = await this._paced.ReplaceAway(id, first, this._cancellation);

        // Assert
        Assert.Equal<(SeatController, AwayVoting?, bool)>(
            (new BotSeat(FirstChoiceBot.BotId), null, true),
            (replaced.Record.Controllers[0], replaced.Away, replaced.Record.HasChangedHands)
        );
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task ReplaceAway_WhenAnotherThanTheFirstToArriveAsks_SeatsABotOnlyOnceTheyHaveWaitedLonger(
        bool hasWaited,
        bool replaces
    )
    {
        // Arrange - the first seat, away, arrived first, and missed a turn: its vote is open
        (GameId id, string voter, _) = await this.FirstSeatPassedAway();
        this._time.Pass(hasWaited ? GameServiceSettings.DefaultReplaceWait : TimeSpan.Zero);

        // Act
        Exception? refused = await Record.ExceptionAsync(() => this._paced.ReplaceAway(id, voter, this._cancellation));
        GameSnapshot after = await this._paced.Get(id, this._cancellation);

        // Assert
        Assert.Equal(
            (replaces, !replaces),
            (after.Record.Controllers[0] is BotSeat, refused is TooSoonToReplaceException)
        );
    }

    [Fact]
    public async Task ReplaceAway_WhenTheSeatAwayMissedATurnAndAnotherDecides_SeatsABotThereAfterTheWait()
    {
        // Arrange - the first seat, away, was passed; the second, its voter, decides now
        (GameId id, string voter, _) = await this.FirstSeatAway();
        this._time.Pass(Grace);
        GameSnapshot voted = await this._paced.Submit(id, voter, StopWaitingFor(First), this._cancellation);
        GameSnapshot passed = await this._paced.Advance(id, voted.Record.Version, this._cancellation);
        this._time.Pass(GameServiceSettings.DefaultReplaceWait);

        // Act
        GameSnapshot replaced = await this._paced.ReplaceAway(id, voter, this._cancellation);

        // Assert
        Assert.Equal(
            (Second, First, true, true),
            (
                passed.State.DecidingSeat,
                passed.Away!.Absent,
                passed.Away.IsOpen,
                replaced.Record.Controllers[0] is BotSeat
            )
        );
    }

    [Fact]
    public async Task Advance_WhenASeatIsAway_SavesItAwayUntilABotTakesItsPlace()
    {
        // Arrange - the first seat, away, was passed; the second, its voter, decides now
        (GameId id, string voter, _) = await this.FirstSeatAway();
        this._time.Pass(Grace);
        GameSnapshot voted = await this._paced.Submit(id, voter, StopWaitingFor(First), this._cancellation);

        // Act
        GameSnapshot passed = await this._paced.Advance(id, voted.Record.Version, this._cancellation);
        this._time.Pass(GameServiceSettings.DefaultReplaceWait);
        GameSnapshot replaced = await this._paced.ReplaceAway(id, voter, this._cancellation);

        // Assert
        Assert.Equal(
            ($"{First.Value}", string.Empty),
            (
                string.Join(",", passed.Record.Away.Select(seat => seat.Value)),
                string.Join(",", replaced.Record.Away.Select(seat => seat.Value))
            )
        );
    }

    [Fact]
    public async Task ReplaceAway_WhenTheGraceIsNotOver_Refuses()
    {
        // Arrange - the second seat arrived first, but the first has had no time yet
        GameId id =
            await this._paced.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string first = await this._paced.Claim(id, Second, this._cancellation);
        _ = await this._paced.Claim(id, First, this._cancellation);

        // Act
        Task<GameSnapshot> replacing = this._paced.ReplaceAway(id, first, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<TooSoonToReplaceException>(() => replacing);
    }

    [Fact]
    public async Task ReplaceAway_WhenABotDecides_Refuses()
    {
        // Arrange
        GameId id = await this._paced.Create(TinyRules.RulesetId, [Bot, SeatPlan.Open], this._cancellation);
        string token = await this._paced.Claim(id, Second, this._cancellation);

        // Act
        Task<GameSnapshot> replacing = this._paced.ReplaceAway(id, token, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<TooSoonToReplaceException>(() => replacing);
    }

    [Fact]
    public async Task ReplaceAway_WhenTheTokenHoldsNoSeat_Refuses()
    {
        // Arrange
        (GameId id, _, _) = await this.FirstSeatAway();

        // Act
        Task<GameSnapshot> replacing = this._paced.ReplaceAway(id, "no-ones", this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<NotAPlayerException>(() => replacing);
    }

    [Fact]
    public async Task ReplaceAway_WhenNoBotIsLeftToSeat_LeavesTheGameAsItIs()
    {
        // Arrange - the second seat arrived first; the first, deciding, is away
        GameService service = this.Service(GameServiceSettings.Default, bots: []);
        GameId id = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        string first = await service.Claim(id, Second, this._cancellation);
        _ = await service.Claim(id, First, this._cancellation);
        GameSnapshot before = await service.Get(id, this._cancellation);
        this._time.Pass(Grace);

        // Act
        GameSnapshot after = await service.ReplaceAway(id, first, this._cancellation);

        // Assert
        Assert.Equal(before.Record.Version, after.Record.Version);
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

    /// <summary>A game of Tiny, its second seat open, once the person holding the first has passed.</summary>
    private static async Task<(GameId Id, string Token)> FirstPassed(
        GameService service,
        CancellationToken cancellationToken
    )
    {
        GameId id = await service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], cancellationToken);
        string token = await Sit(service, id, First, cancellationToken);
        _ = await service.Submit(id, token, Pass(First), cancellationToken);
        return (id, token);
    }

    /// <summary>A game of Tiny, its every seat open.</summary>
    private Task<GameId> CreateOpen() =>
        this._service.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);

    /// <summary>A game of <see cref="TinyRules"/> not over, its first seat a signed-in player's.</summary>
    private static GameRecord Waiting(string id, string player, DateTimeOffset changed) =>
        new(
            new GameId(id),
            TinyRules.RulesetId,
            [new HumanSeat("hash", new PlayerId(player)), new OpenSeat()],
            [],
            1
        )
        {
            IsOver = false,
            LastChanged = changed,
        };

    private GameService Service(
        GameServiceSettings settings,
        InMemoryGameStore? store = null,
        ImmutableList<IBotPlaying>? bots = null,
        bool isDealt = false,
        bool hasFaceOff = false,
        bool isDraftable = true
    ) =>
        new(
            new RulesetCatalog(
                [
                    new TinyRules(GameLength, isDealt, hasFaceOff: hasFaceOff) { IsDraftable = isDealt && isDraftable },
                    new RollingRules(GameLength),
                ]
            ),
            new BotCatalog(bots ?? [new FirstChoiceBot()]),
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
            settings,
            this._players,
            this._gate
        );
}
