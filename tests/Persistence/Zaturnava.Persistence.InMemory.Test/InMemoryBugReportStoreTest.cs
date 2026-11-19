using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Reports;

namespace Net.NowhereAtAll.Zaturnava.Persistence.InMemory.Test;

public sealed class InMemoryBugReportStoreTest
{
    private readonly InMemoryBugReportStore _store = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task All_WhenReportsWereSent_ReadsThemInTheOrderSent()
    {
        // Arrange
        BugReport first = Report("a");
        BugReport second = Report("b");
        await this._store.Add(first, this._cancellation);
        await this._store.Add(second, this._cancellation);

        // Act
        ImmutableList<BugReport> all = await this._store.All(this._cancellation);

        // Assert
        Assert.Equal([first, second], all);
    }

    private static BugReport Report(string id) =>
        new(id, new PlayerId("ada"), new GameId("game"), "chaturaji-2027", 1, 0, "Odd.", DateTimeOffset.UnixEpoch);
}
