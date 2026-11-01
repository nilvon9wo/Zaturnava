using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;

namespace Net.NowhereAtAll.Zaturnava.Persistence.InMemory.Test;

public sealed class InMemoryGameStoreTest
{
    private readonly InMemoryGameStore _store = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Find_WhenTheGameWasAdded_ReturnsIt()
    {
        // Arrange
        GameRecord record = Record("first");
        await this._store.Add(record, this._cancellation);

        // Act
        GameRecord? found = await this._store.Find(record.Id, this._cancellation);

        // Assert
        Assert.Equal(record, found);
    }

    [Fact]
    public async Task Find_WhenTheGameIsUnknown_ReturnsNull()
    {
        // Arrange - nothing beyond the empty store

        // Act
        GameRecord? found = await this._store.Find(new GameId("missing"), this._cancellation);

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public async Task List_WhenGamesWereAdded_ReturnsThemInIdOrder()
    {
        // Arrange
        await this._store.Add(Record("b"), this._cancellation);
        await this._store.Add(Record("a"), this._cancellation);

        // Act
        ImmutableList<GameRecord> games = await this._store.List(this._cancellation);

        // Assert
        Assert.Equal(["a", "b"], games.Select(game => game.Id.Value));
    }

    [Theory]
    [InlineData(null, null, null, "b,c,a")]
    [InlineData(true, null, null, "a")]
    [InlineData(false, null, null, "b,c")]
    [InlineData(null, true, null, "b,a")]
    [InlineData(null, false, null, "c")]
    [InlineData(null, null, false, "b,a")]
    [InlineData(null, null, true, "c")]
    public async Task Page_WhenFiltered_ListsTheOfferedGamesThatMatchNewestFirst(
        bool? open,
        bool? peopleOnly,
        bool? over,
        string expected
    )
    {
        // Arrange
        await this.AddListed();

        // Act
        GamePage page = await this._store.Page(new GameQuery(null, open, peopleOnly, over), Tiny, this._cancellation);

        // Assert
        Assert.Equal(expected, string.Join(",", page.Games.Select(game => game.Id.Value)));
    }

    [Fact]
    public async Task Page_WhenAskedForALaterPage_ListsItsGamesAndCountsThemAll()
    {
        // Arrange
        await this.AddListed();

        // Act
        GamePage page = await this._store.Page(new GameQuery(Page: 2, PageSize: 2), Tiny, this._cancellation);

        // Assert
        GameListing listed = Assert.Single(page.Games);
        Assert.Equal(("a", 3, 1, false), (listed.Id.Value, page.Total, listed.Moves, listed.IsOver));
    }

    [Fact]
    public async Task Unsummarized_WhenSomeGamesAreNotKnownToBeOver_ReturnsThem()
    {
        // Arrange
        await this.AddListed();

        // Act
        ImmutableList<GameRecord> games = await this._store.Unsummarized(this._cancellation);

        // Assert
        Assert.Equal("a", Assert.Single(games).Id.Value);
    }

    [Fact]
    public async Task MarkOver_WhenTheGameIsStored_KeepsWhetherItIsOver()
    {
        // Arrange
        await this.AddListed();

        // Act
        await this._store.MarkOver(new GameId("a"), true, this._cancellation);

        // Assert
        Assert.Equal(true, (await this._store.Find(new GameId("a"), this._cancellation))?.IsOver);
    }

    [Fact]
    public async Task Add_WhenTheIdIsTaken_ThrowsConcurrencyConflictException()
    {
        // Arrange
        await this._store.Add(Record("taken"), this._cancellation);

        // Act
        Task adding = this._store.Add(Record("taken"), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => adding);
    }

    [Fact]
    public async Task Replace_WhenTheVersionIsNext_StoresTheNewRecord()
    {
        // Arrange
        GameRecord record = Record("game");
        await this._store.Add(record, this._cancellation);
        await this._store.Replace(record.Next(), this._cancellation);

        // Act
        GameRecord? found = await this._store.Find(record.Id, this._cancellation);

        // Assert
        Assert.Equal(1, found?.Version);
    }

    [Fact]
    public async Task Replace_WhenTheVersionIsStale_ThrowsConcurrencyConflictException()
    {
        // Arrange
        GameRecord record = Record("game");
        await this._store.Add(record, this._cancellation);

        // Act
        Task replacing = this._store.Replace(record, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => replacing);
    }

    [Fact]
    public async Task Replace_WhenTheGameIsUnknown_ThrowsConcurrencyConflictException()
    {
        // Arrange - nothing beyond the empty store

        // Act
        Task replacing = this._store.Replace(Record("missing").Next(), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => replacing);
    }

    private static readonly ImmutableHashSet<string> Tiny = ["tiny"];
    private static readonly ImmutableSortedDictionary<string, string> Arguments =
        ImmutableSortedDictionary<string, string>.Empty;

    private static GameRecord Record(string id) => new(new GameId(id), "tiny", [new OpenSeat()], [], 0);

    /// <summary>
    /// Stores an open game with a move and not yet known to be over (a), a person's game going on (b), a bot's game
    /// over (c), each changed later than the one before but b last, and a game of a ruleset not offered (d).
    /// </summary>
    private async Task AddListed()
    {
        DateTimeOffset start = DateTimeOffset.UnixEpoch;
        GameRecord[] games =
        [
            Record("a") with { Actions = [new ActionDescriptor("pass", Arguments)], LastChanged = start },
            Record("b") with { Controllers = [new HumanSeat("hash")], IsOver = false, LastChanged = start.AddHours(2) },
            Record("c") with { Controllers = [new BotSeat("random")], IsOver = true, LastChanged = start.AddHours(1) },
            Record("d") with { RulesetId = "retired", IsOver = false, LastChanged = start.AddHours(3) },
        ];
        foreach (GameRecord game in games)
        {
            await this._store.Add(game, this._cancellation);
        }
    }
}
