using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Reports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Reports;

/// <summary>Bug reports about a game of <see cref="TinyRules"/>: two seats, Ada's and Bea's.</summary>
public sealed class BugReportServiceTest
{
    private static readonly SeatId First = new(0);
    private static readonly SeatId Second = new(1);
    private static readonly BugReportSeen Seen = new(3, 4, 5);

    private readonly SteppedTime _time = new();
    private readonly InMemoryBugReportStore _store = new();
    private readonly GameService _games;
    private readonly BugReportService _reports;
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    public BugReportServiceTest()
    {
        DecisionClock clock = new(this._time);
        this._games = new GameService(
            new RulesetCatalog([new TinyRules(6)]),
            new BotCatalog([new FirstChoiceBot()]),
            ScenarioCatalog.None,
            new InMemoryGameStore(),
            new RecordingNotifier(),
            new LoadedDice(4),
            clock,
            GameServiceSettings.Default,
            new InMemoryPlayerStore(),
            new GameGate()
        );
        this._reports = new BugReportService(this._games, this._store, clock);
    }

    [Theory]
    [InlineData("bea", 1)]
    [InlineData("cy", null)]
    public async Task Report_WhenAPlayerReports_KeepsWhatTheySayWithTheGameAsItStandsAndTheirSeatIfAny(
        string reporter,
        int? seat
    )
    {
        // Arrange - Ada at the first seat, Bea at the second; Cy only watching
        GameId id = await this.Seated();
        GameRecord record = (await this._games.Get(id, this._cancellation)).Record;

        // Act
        BugReport report = await this._reports.Report(
            id,
            new PlayerId(reporter),
            "  The Guard is outside!  ",
            Seen,
            this._cancellation
        );

        // Assert
        Assert.Equal(
            (
                "The Guard is outside!",
                record.Version,
                record.Actions.Count,
                First,
                false,
                seat is { } held ? new SeatId(held) : null,
                Seen,
                this._time.GetUtcNow(),
                1
            ),
            (
                report.Description,
                report.Version,
                report.Actions,
                report.Deciding,
                report.IsOver,
                report.Seat,
                report.Seen,
                report.At,
                (await this._store.All(this._cancellation)).Count(kept => kept == report)
            )
        );
    }

    [Fact]
    public async Task Report_WhenItSaysTooMuch_ThrowsInvalidBugReportExceptionAndKeepsNothing()
    {
        // Arrange
        GameId id = await this.Seated();
        string words = new('x', BugReportService.MaximumLength + 1);

        // Act
        Task reporting = this._reports.Report(id, new PlayerId("ada"), words, Seen, this._cancellation);

        // Assert
        InvalidBugReportException exception = await Assert.ThrowsAsync<InvalidBugReportException>(() => reporting);
        Assert.Equal(
            (GameRequestFailure.Invalid, 0),
            (exception.Failure, (await this._store.All(this._cancellation)).Count)
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Find_WhenAskedByIdOrByOneNeverGiven_IsThatReportOrNone(bool isKnown)
    {
        // Arrange
        GameId id = await this.Seated();
        BugReport report = await this._reports.Report(id, new PlayerId("ada"), "Odd.", Seen, this._cancellation);

        // Act
        BugReport? found = await this._reports.Find(isKnown ? report.Id : "never", this._cancellation);

        // Assert
        Assert.Equal(isKnown ? report : null, found);
    }

    [Fact]
    public async Task Report_WhenTheGameIsUnknown_ThrowsGameNotFoundException()
    {
        // Arrange - nothing beyond the service

        // Act
        Task reporting =
            this._reports.Report(new GameId("missing"), new PlayerId("ada"), "Gone?", Seen, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<GameNotFoundException>(() => reporting);
    }

    private async Task<GameId> Seated()
    {
        GameId id = await this._games.Create(TinyRules.RulesetId, [SeatPlan.Open, SeatPlan.Open], this._cancellation);
        _ = await this._games.Claim(id, First, new PlayerId("ada"), null, this._cancellation);
        _ = await this._games.Claim(id, Second, new PlayerId("bea"), null, this._cancellation);
        return id;
    }
}
