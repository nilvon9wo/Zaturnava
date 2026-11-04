using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Players;

public sealed class StandingServiceTest
{
    private static readonly PlayerId Ada = new("ada");
    private static readonly PlayerId Bea = new("bea");
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);

    private readonly InMemoryGameStore _games = new();
    private readonly InMemoryPlayerStore _players = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task RateFinished_WhenTwoPlayersBeatABotAndAGuest_RatesEachAgainstTheTeamsMeanAndAddsTheirPoints()
    {
        // Arrange - Ada (5 points) and Bea (4) beat a bot rated 1700 (3) and a guest (0)
        GameState ended = EndedRules.State([Black]) with
        {
            Scores = ImmutableDictionary<SeatId, int>.Empty
                .Add(Black, 5)
                .Add(Yellow, 3)
                .Add(new SeatId(2), 4),
        };
        GameRecord game = Record(
            "g",
            new HumanSeat("ada", Ada),
            new BotSeat(FirstChoiceBot.BotId),
            new HumanSeat("bea", Bea),
            new HumanSeat("guest")
        );
        await this._games.Add(game, this._cancellation);
        StandingService service = this.Service(new EndedRules(ended), 1700);

        // Act
        int rated = await service.RateFinished(this._cancellation);

        // Assert - expected 1 / (1 + 10^(100 / 400)) against the mean of 1700 and 1500
        double elo = Standing.FirstElo + (StandingService.K * (1 - (1 / (1 + Math.Pow(10, 0.25)))));
        ImmutableDictionary<PlayerId, Standing> standings =
            await this._players.Standings(EndedRules.RulesetId, [Ada, Bea], this._cancellation);
        Assert.Equal(
            (1, 5 + 9, 4 + 9, 1, 1, 0),
            (
                rated,
                standings[Ada].Experience,
                standings[Bea].Experience,
                standings[Ada].Won,
                standings[Bea].Won,
                (await this._games.Unrated(10, this._cancellation)).Count
            )
        );
        Assert.Equal(elo, standings[Ada].Elo, 9);
        Assert.Equal(elo, standings[Bea].Elo, 9);
    }

    [Theory]
    [InlineData(false, 1500 - 16, 0, 1, 0)]
    [InlineData(true, 1500, 0, 0, 1)]
    public async Task RateFinished_WhenAPlayerLosesOrDrawsEvenly_MovesTheirEloByHalfOfKOrNotAtAll(
        bool isDrawn,
        double elo,
        int won,
        int lost,
        int drawn
    )
    {
        // Arrange - Ada, who has won before, and a bot no longer offered, against Bea's AI and a bot not rated here
        await this._players.Keep(
            [new Standing(Ada, EndedRules.RulesetId) { Won = 3, Experience = 20 }],
            this._cancellation
        );
        GameState ended = EndedRules.State(isDrawn ? [] : [Yellow]);
        await this._games.Add(
            Record(
                "g",
                new HumanSeat("ada", Ada),
                new HumanSeat("bea", Bea, IsAi: true),
                new BotSeat("gone"),
                new BotSeat(FirstChoiceBot.BotId)
            ),
            this._cancellation
        );
        StandingService service = this.Service(new EndedRules(ended), null);

        // Act
        _ = await service.RateFinished(this._cancellation);

        // Assert
        ImmutableDictionary<PlayerId, Standing> standings =
            await this._players.Standings(EndedRules.RulesetId, [Ada, Bea], this._cancellation);
        Assert.Equal(
            [KeyValuePair.Create(Ada, new Standing(Ada, EndedRules.RulesetId)
            {
                Elo = elo,
                Experience = 20,
                Won = 3 + won,
                Lost = lost,
                Drawn = drawn,
            })],
            standings
        );
    }

    [Theory]
    [InlineData("retired", true)]
    [InlineData(EndedRules.RulesetId, false)]
    public async Task RateFinished_WhenAGameCannotBeReplayedToItsEnd_CountsItForNoOneButLetsItBe(
        string ruleset,
        bool hasEnded
    )
    {
        // Arrange - a ruleset no longer offered, or a game its rules replay to no outcome
        GameState state = hasEnded ? EndedRules.State([Black]) : EndedRules.State([Black]) with { Outcome = null };
        await this._games.Add(
            Record("g", new HumanSeat("ada", Ada), new HumanSeat("bea", Bea)) with { RulesetId = ruleset },
            this._cancellation
        );
        StandingService service = this.Service(new EndedRules(state), null);

        // Act
        int rated = await service.RateFinished(this._cancellation);

        // Assert
        Assert.Equal(
            (1, 0, 0),
            (
                rated,
                (await this._players.Leaders(EndedRules.RulesetId, 10, this._cancellation)).Count,
                (await this._games.Unrated(10, this._cancellation)).Count
            )
        );
    }

    [Fact]
    public async Task Leaders_WhenSomeoneStandingIsNoLongerKnown_NamesTheRestBestFirst()
    {
        // Arrange
        await this._players.Add(new Player(Ada, "Ada", []), this._cancellation);
        await this._players.Add(new Player(Bea, "Bea", []), this._cancellation);
        await this._players.Keep(
            [
                new Standing(Ada, "chess") { Elo = 1450 },
                new Standing(Bea, "chess") { Elo = 1550 },
                new Standing(new PlayerId("ghost"), "chess") { Elo = 1600 },
            ],
            this._cancellation
        );
        StandingService service = this.Service(new EndedRules(EndedRules.State([])), null);

        // Act
        ImmutableList<Leader> leaders = await service.Leaders("chess", this._cancellation);

        // Assert
        Assert.Equal("Bea 1550, Ada 1450", string.Join(", ", leaders.Select(l => $"{l.Name} {l.Standing.Elo}")));
    }

    private static GameRecord Record(string id, params SeatController[] seats) =>
        new(new GameId(id), EndedRules.RulesetId, [.. seats], [], 1)
        {
            IsOver = true,
            LastChanged = DateTimeOffset.UnixEpoch,
        };

    /// <summary>The service, the first-choice bot rated <paramref name="botRating"/> at the rules, if at all.</summary>
    private StandingService Service(EndedRules rules, int? botRating) =>
        new(
            new RulesetCatalog([rules]),
            new BotCatalog(
                [new FirstChoiceBot()],
                botRating is { } rating
                    ? ImmutableDictionary<string, ImmutableDictionary<string, int>>.Empty.Add(
                        FirstChoiceBot.BotId,
                        ImmutableDictionary<string, int>.Empty.Add(EndedRules.RulesetId, rating)
                    )
                    : []
            ),
            this._games,
            this._players
        );
}

/// <summary>
/// Rules of a game of four seats, 0 and 2 against 1 and 3, that is over before it begins: replayed, every game of
/// them is <paramref name="ended"/>.
/// </summary>
internal sealed class EndedRules(GameState ended) : IRulesApplying
{
    public const string RulesetId = "ended";

    private readonly GameState _ended = ended;

    public string Id => RulesetId;

    public string Name => "Ended";

    public ICellNavigating Topology => throw new NotSupportedException();

    public ImmutableDictionary<SeatId, CellId> Thrones => throw new NotSupportedException();

    /// <summary>The game over, won by <paramref name="winners"/>; drawn, if by none.</summary>
    public static GameState State(ImmutableList<SeatId> winners) =>
        new(
            Position.Empty,
            [.. Enumerable.Range(0, 4).Select(seat => new Seat(new SeatId(seat), new Direction(0, 1)))],
            TeamGraph.FromTeams([[new SeatId(0), new SeatId(2)], [new SeatId(1), new SeatId(3)]]),
            null,
            1,
            [],
            []
        )
        {
            Outcome = new GameOutcome(winners, "test", "It ended."),
        };

    public GameState CreateInitialState() => this._ended;

    public ImmutableList<GameAction> LegalActions([Required] GameState state) => [];

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        throw new NotSupportedException();

    public bool MayOffer([Required] GameState state) => throw new NotSupportedException();

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) =>
        throw new NotSupportedException();

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => throw new NotSupportedException();

    public bool MayRejoin([Required] GameState state, SeatId seat) => throw new NotSupportedException();

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) =>
        throw new NotSupportedException();

    public DecisionTimer? TimerOf([Required] GameState state) => null;

    public GameAction Parse([Required] ActionDescriptor descriptor) => throw new NotSupportedException();

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();

    public int WorthOf(PieceKindId kind) => throw new NotSupportedException();

    public int RansomPointsOf([Required] GameState state, [Required] RansomOffer offer) =>
        throw new NotSupportedException();

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) =>
        throw new NotSupportedException();

    public bool IsLeader(PieceKindId kind) => throw new NotSupportedException();

    public bool IsHeavy(PieceKindId kind) => throw new NotSupportedException();

    public string NameOfDie(int value) => throw new NotSupportedException();

    public bool MayUseDie([Required] GameState state, int value) => throw new NotSupportedException();
}
