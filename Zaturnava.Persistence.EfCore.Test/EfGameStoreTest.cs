using System.Collections.Immutable;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.EntityFrameworkCore;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test.Rows;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test;

public sealed class EfGameStoreTest : IDisposable
{
    private readonly SqliteGameDatabase _database = new();
    private readonly EfGameStore _store;
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    public EfGameStoreTest() => this._store = new EfGameStore(this._database.NewContext());

    public void Dispose() => this._database.Dispose();

    [Fact]
    public async Task Find_WhenRowsForAGameExist_RebuildsItsRecord()
    {
        // Arrange
        ActionEntity action = await this.Rows<ActionEntity>()
            .SetOverrideTemplate(new ActionEntity { Kind = PassAction.Kind })
            .Supply();

        // Act
        GameRecord? record = await this._store.Find(new GameId(action.GameId), this._cancellation);

        // Assert
        Assert.Equal(
            (GameRowProvider.DefaultRulesetId, new PassAction(new SeatId(0)).Describe().Arguments.Count),
            (record?.RulesetId, record?.Actions.Single().Arguments.Count)
        );
    }

    [Fact]
    public async Task Find_WhenASeatRowExists_RestoresItsController()
    {
        // Arrange
        SeatEntity seat = await this.Rows<SeatEntity>()
            .SetOverrideTemplate(new SeatEntity { Kind = BotSeat.KindName, Detail = "random" })
            .Supply();

        // Act
        GameRecord? record = await this._store.Find(new GameId(seat.GameId), this._cancellation);

        // Assert
        Assert.Equal([new BotSeat("random")], record?.Controllers ?? []);
    }

    [Fact]
    public async Task Find_WhenTheGameIsUnknown_ReturnsNull()
    {
        // Arrange - nothing beyond the empty database

        // Act
        GameRecord? record = await this._store.Find(new GameId("missing"), this._cancellation);

        // Assert
        Assert.Null(record);
    }

    [Fact]
    public async Task List_WhenGamesExist_ReturnsThemInIdOrder()
    {
        // Arrange
        _ = await this.Rows<GameEntity>()
            .SetOverrideTemplateList([new GameEntity { Id = "b" }, new GameEntity { Id = "a" }])
            .SupplyList();

        // Act
        ImmutableList<GameRecord> games = await this._store.List(this._cancellation);

        // Assert
        Assert.Equal(["a", "b"], games.Select(game => game.Id.Value));
    }

    [Fact]
    public async Task Add_WhenTheGameIsNew_StoresItWithItsSeatsAndActions()
    {
        // Arrange
        GameRecord record = Record("new");

        // Act
        await this._store.Add(record, this._cancellation);

        // Assert
        GameRecord? reread = await new EfGameStore(this._database.NewContext()).Find(record.Id, this._cancellation);
        Assert.Equivalent(record, reread);
    }

    [Fact]
    public async Task Add_WhenTheIdIsTaken_ThrowsConcurrencyConflictException()
    {
        // Arrange
        await this._store.Add(Record("taken"), this._cancellation);
        EfGameStore secondRequest = new(this._database.NewContext());

        // Act
        Task adding = secondRequest.Add(Record("taken"), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => adding);
    }

    [Fact]
    public async Task Replace_WhenTheVersionIsNext_UpdatesSeatsAndAppendsActions()
    {
        // Arrange
        GameRecord record = Record("game");
        await this._store.Add(record, this._cancellation);
        GameRecord changed = record.Next() with
        {
            Controllers = [new HumanSeat("HASH"), new OpenSeat()],
            Actions = record.Actions.Add(new PassAction(new SeatId(1)).Describe()),
        };

        // Act
        await new EfGameStore(this._database.NewContext()).Replace(changed, this._cancellation);

        // Assert
        GameRecord? reread = await new EfGameStore(this._database.NewContext()).Find(record.Id, this._cancellation);
        Assert.Equivalent(changed, reread);
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
        // Arrange - nothing beyond the empty database

        // Act
        Task replacing = this._store.Replace(Record("missing").Next(), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => replacing);
    }

    private static GameRecord Record(string id) =>
        new(
            new GameId(id),
            GameRowProvider.DefaultRulesetId,
            [new OpenSeat(), new BotSeat("random")],
            [new PassAction(new SeatId(0)).Describe()],
            0
        );

    private RecordProvider<TRow> Rows<TRow>()
        where TRow : class =>
        new RecordProvider<TRow>(new RowProviderLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this._database.Context));
}
