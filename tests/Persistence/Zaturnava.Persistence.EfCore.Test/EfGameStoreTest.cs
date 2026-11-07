using System.Collections.Immutable;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.EntityFrameworkCore;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
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
    public async Task Find_WhenTheGameWasAddedWithItsTimes_RestoresThem()
    {
        // Arrange
        GameRecord record = Record("timed") with
        {
            Created = DateTimeOffset.UnixEpoch,
            LastChanged = DateTimeOffset.UnixEpoch.AddHours(1),
        };
        await this._store.Add(record, this._cancellation);

        // Act
        GameRecord? reread = await new EfGameStore(this._database.NewContext()).Find(record.Id, this._cancellation);

        // Assert
        Assert.Equal((record.Created, record.LastChanged), (reread?.Created, reread?.LastChanged));
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

    [Theory]
    [InlineData(null, null, null, "b,c,a")]
    [InlineData(true, null, null, "a")]
    [InlineData(false, null, null, "b,c")]
    [InlineData(null, true, null, "b")]
    [InlineData(null, false, null, "c,a")]
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
        GameQuery query = new(null, open, peopleOnly, over);

        // Act
        GamePage page = await this._store.Page(query, Offered, this._cancellation);

        // Assert
        Assert.Equal(expected, string.Join(",", page.Games.Select(game => game.Id.Value)));
    }

    [Fact]
    public async Task Page_WhenAskedForALaterPage_ListsItsGamesWithTheirSeatsAndCountsThemAll()
    {
        // Arrange
        await this.AddListed();

        // Act
        GamePage page = await this._store.Page(new GameQuery(Page: 2, PageSize: 2), Offered, this._cancellation);

        // Assert
        GameListing listed = Assert.Single(page.Games);
        Assert.Equal(
            ("a", 3, 1, false, new BotSeat("random")),
            (listed.Id.Value, page.Total, listed.Moves, listed.IsOver, listed.Controllers[1])
        );
        Assert.Equal(
            ((DateTimeOffset?)DateTimeOffset.UnixEpoch.AddHours(-1), (DateTimeOffset?)DateTimeOffset.UnixEpoch),
            (listed.Created, listed.LastChanged)
        );
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
        EfGameStore rereading = new(this._database.NewContext());
        Assert.Equal(true, (await rereading.Find(new GameId("a"), this._cancellation))?.IsOver);
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
            IsOver = true,
            LastChanged = DateTimeOffset.UnixEpoch.AddDays(1),
        };

        // Act
        await new EfGameStore(this._database.NewContext()).Replace(changed, this._cancellation);

        // Assert
        GameRecord? reread = await new EfGameStore(this._database.NewContext()).Find(record.Id, this._cancellation);
        Assert.Equivalent(changed, reread);
    }

    [Fact]
    public async Task Replace_WhenASeatChangesHands_KeepsThatItHas()
    {
        // Arrange
        GameRecord record = Record("changed-hands");
        await this._store.Add(record, this._cancellation);

        // Act
        await new EfGameStore(this._database.NewContext())
            .Replace(record.Next() with { HasChangedHands = true }, this._cancellation);

        // Assert
        GameRecord? reread = await new EfGameStore(this._database.NewContext()).Find(record.Id, this._cancellation);
        Assert.True(reread?.HasChangedHands);
    }

    [Fact]
    public async Task Replace_WhenASeatGoesAway_KeepsItAwayInTheGameAndItsListing()
    {
        // Arrange
        GameRecord record = Record("away");
        await this._store.Add(record, this._cancellation);
        GameRecord changed = record.Next() with { Away = [new SeatId(0)] };

        // Act
        await new EfGameStore(this._database.NewContext()).Replace(changed, this._cancellation);

        // Assert
        EfGameStore rereading = new(this._database.NewContext());
        GameRecord? reread = await rereading.Find(record.Id, this._cancellation);
        GamePage page = await rereading.Page(new GameQuery(), Offered, this._cancellation);
        Assert.Equal([new SeatId(0)], reread?.Away ?? []);
        Assert.Equal([new SeatId(0)], page.Games.Single().Away);
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

    [Fact]
    public async Task Idle_WhenGamesChangedBeforeAndAfter_ReturnsThoseNotOverUnchangedSince()
    {
        // Arrange - c is over, d changed later, and e from before when it changed was kept
        await this.AddListed();
        await this._store.Add(Record("e"), this._cancellation);

        // Act
        ImmutableList<GameRecord> idle =
            await this._store.Idle(DateTimeOffset.UnixEpoch.AddHours(2.5), this._cancellation);

        // Assert
        Assert.Equal(
            ("a,b", "HASH"),
            (
                string.Join(",", idle.Select(game => game.Id.Value).Order(StringComparer.Ordinal)),
                ((HumanSeat)idle.Single(game => game.Id.Value == "b").Controllers[0]).TokenHash
            )
        );
    }

    [Fact]
    public async Task Remove_WhenGamesAreNamed_LetsGoOfThemWithTheirSeatsAndActions()
    {
        // Arrange
        await this.AddListed();

        // Act
        await this._store.Remove([new GameId("a"), new GameId("c"), new GameId("missing")], this._cancellation);

        // Assert
        await using GameDbContext context = this._database.NewContext();
        Assert.Equal(
            ("b,d", 3, 1),
            (
                string.Join(",", context.Games.OrderBy(game => game.Id).Select(game => game.Id)),
                context.Set<SeatEntity>().Count(),
                context.Set<ActionEntity>().Count()
            )
        );
    }

    [Fact]
    public async Task ClaimedBy_WhenAPlayerClaimedSeats_ListsTheGamesNotOverTheyClaimedOne()
    {
        // Arrange - Bea claimed a seat at e (their AI plays it) and at f, which is over; a guest called bea is at g
        PlayerId bea = new("bea");
        await this.AddListed();
        await this._store.Add(
            Record("e") with { Controllers = [new HumanSeat("one", bea, IsAi: true), new OpenSeat()] },
            this._cancellation
        );
        await this._store.Add(
            Record("f") with { Controllers = [new HumanSeat("two", bea)], IsOver = true },
            this._cancellation
        );
        await this._store.Add(
            Record("g") with { Controllers = [new HumanSeat("three", Nickname: "bea")] },
            this._cancellation
        );

        // Act
        ImmutableList<GameRecord> claimed =
            await new EfGameStore(this._database.NewContext()).ClaimedBy(bea, this._cancellation);

        // Assert
        Assert.Equal(["e"], claimed.Select(game => game.Id.Value));
    }

    [Fact]
    public async Task Reclaim_WhenAPlayerClaimedSeats_GivesThemToTheOtherAndLeavesTheRestAsTheyWere()
    {
        // Arrange - a guest named so that her seat's detail ends as Bea's id does
        PlayerId bea = new("bea");
        PlayerId ada = new("ada");
        await this._store.Add(
            Record("g") with
            {
                Controllers =
                [
                    new HumanSeat("one", bea, IsAi: true),
                    new HumanSeat("two", ada),
                    new HumanSeat("three", Nickname: "bea"),
                    new BotSeat("random"),
                ],
            },
            this._cancellation
        );

        // Act
        await new EfGameStore(this._database.NewContext()).Reclaim(bea, ada, this._cancellation);

        // Assert
        GameRecord? game = await new EfGameStore(this._database.NewContext()).Find(new GameId("g"), this._cancellation);
        Assert.Equal(
            [
                new HumanSeat("one", ada, IsAi: true),
                new HumanSeat("two", ada),
                new HumanSeat("three", Nickname: "bea"),
                new BotSeat("random"),
            ],
            game?.Controllers ?? []
        );
    }

    [Fact]
    public async Task Unrated_WhenGamesAreOver_ReadsThoseNotYetRatedEarliestChangedFirstThenById()
    {
        // Arrange - c over an hour in; x, w and v over from the start, but v rated
        await this.AddListed();
        foreach (string id in new[] { "x", "w", "v" })
        {
            await this._store.Add(
                Record(id) with { IsOver = true, LastChanged = DateTimeOffset.UnixEpoch },
                this._cancellation
            );
        }

        await this._store.MarkRated(new GameId("v"), this._cancellation);

        // Act
        ImmutableList<GameRecord> unrated =
            await new EfGameStore(this._database.NewContext()).Unrated(3, this._cancellation);

        // Assert
        Assert.Equal(
            ("w,x,c", 1),
            (string.Join(",", unrated.Select(game => game.Id.Value)), unrated[0].Actions.Count)
        );
    }

    private static GameRecord Record(string id) =>
        new(
            new GameId(id),
            GameRowProvider.DefaultRulesetId,
            [new OpenSeat(), new BotSeat("random")],
            [new PassAction(new SeatId(0)).Describe()],
            0
        );

    private static readonly ImmutableHashSet<string> Offered = [GameRowProvider.DefaultRulesetId];

    /// <summary>
    /// Stores an open game not yet known to be over (a), a person's game going on (b), a bot's game over (c), each
    /// changed later than the one before but b last, and a game of a ruleset not offered (d).
    /// </summary>
    private async Task AddListed()
    {
        DateTimeOffset start = DateTimeOffset.UnixEpoch;
        GameRecord[] games =
        [
            Record("a") with { Created = start.AddHours(-1), LastChanged = start },
            Record("b") with
            {
                Controllers = [new HumanSeat("HASH")],
                Actions = [],
                IsOver = false,
                LastChanged = start.AddHours(2),
            },
            Record("c") with { Controllers = [new BotSeat("random")], IsOver = true, LastChanged = start.AddHours(1) },
            Record("d") with { RulesetId = "retired", IsOver = false, LastChanged = start.AddHours(3) },
        ];
        foreach (GameRecord game in games)
        {
            await this._store.Add(game, this._cancellation);
        }
    }

    private RecordProvider<TRow> Rows<TRow>()
        where TRow : class =>
        new RecordProvider<TRow>(new RowProviderLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new EfPersistenceGateway(this._database.Context));
}
