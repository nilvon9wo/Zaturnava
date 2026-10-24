using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

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

    private static GameRecord Record(string id) => new(new GameId(id), "tiny", [new OpenSeat()], [], 0);
}
