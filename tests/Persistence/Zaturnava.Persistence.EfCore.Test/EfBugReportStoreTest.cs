using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Reports;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test;

public sealed class EfBugReportStoreTest : IDisposable
{
    private static readonly DateTimeOffset At = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteGameDatabase _database = new();
    private readonly EfBugReportStore _store;
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    public EfBugReportStoreTest() => this._store = new EfBugReportStore(this._database.Context);

    public void Dispose() => this._database.Dispose();

    [Fact]
    public async Task All_WhenReportsWereSent_ReadsEachBackWholeTheEarliestFirst()
    {
        // Arrange - one with every fact, one with none of those that may be none
        BugReport later = Report("b", At.AddMinutes(1)) with
        {
            Deciding = new SeatId(2),
            IsOver = true,
            Seat = new SeatId(1),
            Seen = new BugReportSeen(4, 17, 9),
        };
        BugReport earlier = Report("a", At);
        await this._store.Add(later, this._cancellation);
        await this._store.Add(earlier, this._cancellation);

        // Act
        ImmutableList<BugReport> all = await new EfBugReportStore(this._database.NewContext()).All(this._cancellation);

        // Assert
        Assert.Equal([earlier, later], all);
    }

    private static BugReport Report(string id, DateTimeOffset at) =>
        new(id, new PlayerId("ada"), new GameId("game"), "grand-legions-large", 5, 4, "The Guard is outside!", at);
}
