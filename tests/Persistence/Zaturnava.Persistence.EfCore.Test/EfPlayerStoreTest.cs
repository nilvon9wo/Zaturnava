using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test;

public sealed class EfPlayerStoreTest : IDisposable
{
    private static readonly Login GitHub = new("GitHub", "1234");
    private static readonly Login Google = new("Google", "abcd");
    private static readonly Player Ada = new(new PlayerId("ada"), "Ada", [GitHub, Google]);

    private readonly SqliteGameDatabase _database = new();
    private readonly EfPlayerStore _store;
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    public EfPlayerStoreTest() => this._store = new EfPlayerStore(this._database.Context);

    public void Dispose() => this._database.Dispose();

    [Theory]
    [InlineData("GitHub", "1234", true)]
    [InlineData("Google", "abcd", true)]
    [InlineData("GitHub", "abcd", false)]
    public async Task ByLogin_WhenAPlayerWasAdded_ReadsThemBackByEachOfTheirLogins(
        string provider,
        string key,
        bool isFound
    )
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);

        // Act
        Player? found = await new EfPlayerStore(this._database.NewContext())
            .ByLogin(new Login(provider, key), this._cancellation);

        // Assert
        Assert.Equal(isFound ? Described(Ada) : null, Described(found));
    }

    [Fact]
    public async Task Names_WhenAskedOfKnownAndUnknownPlayers_ReadsBackTheKnownOnesNames()
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);

        // Act
        ImmutableDictionary<PlayerId, string> names = await new EfPlayerStore(this._database.NewContext())
            .Names([Ada.Id, new PlayerId("gone"), Ada.Id], this._cancellation);

        // Assert
        Assert.Equal([KeyValuePair.Create(Ada.Id, "Ada")], names);
    }

    [Fact]
    public async Task Standings_WhenKeptAndKeptAgain_ReadBackTheLatestOfTheGameAskedFor()
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);
        await this._store.Keep(
            [new Standing(Ada.Id, "chess") { Elo = 1400 }, new Standing(Ada.Id, "go") { Won = 1 }],
            this._cancellation
        );
        Standing kept = new(Ada.Id, "chess") { Elo = 1520.5, Experience = 7, Won = 2, Lost = 3, Drawn = 4 };
        await new EfPlayerStore(this._database.NewContext()).Keep([kept], this._cancellation);

        // Act
        ImmutableDictionary<PlayerId, Standing> standings = await new EfPlayerStore(this._database.NewContext())
            .Standings("chess", [Ada.Id, new PlayerId("bea"), Ada.Id], this._cancellation);

        // Assert
        Assert.Equal([KeyValuePair.Create(Ada.Id, kept)], standings);
    }

    [Fact]
    public async Task Leaders_WhenPlayersStandAtSeveralGames_ListTheBestOfTheGameByEloThenExperienceThenId()
    {
        // Arrange
        foreach (string id in new[] { "a", "b", "c", "d", "e", "f" })
        {
            await this._store.Add(new Player(new PlayerId(id), id, []), this._cancellation);
        }

        await this._store.Keep(
            [
                new Standing(new PlayerId("c"), "chess") { Elo = 1600, Experience = 5 },
                new Standing(new PlayerId("b"), "chess") { Elo = 1600, Experience = 9 },
                new Standing(new PlayerId("a"), "chess") { Elo = 1600, Experience = 5 },
                new Standing(new PlayerId("d"), "chess") { Elo = 1700 },
                new Standing(new PlayerId("e"), "chess") { Elo = 1100 },
                new Standing(new PlayerId("f"), "go") { Elo = 2000 },
            ],
            this._cancellation
        );

        // Act
        ImmutableList<Standing> leaders =
            await new EfPlayerStore(this._database.NewContext()).Leaders("chess", 4, this._cancellation);

        // Assert
        Assert.Equal("d,b,a,c", string.Join(",", leaders.Select(leader => leader.Player.Value)));
    }

    [Fact]
    public async Task Remove_WhenThePlayerStandsAtAGame_ForgetsWhereTheyStoodAlone()
    {
        // Arrange
        Player bea = new(new PlayerId("bea"), "Bea", []);
        await this._store.Add(Ada, this._cancellation);
        await this._store.Add(bea, this._cancellation);
        await this._store.Keep([new Standing(Ada.Id, "chess"), new Standing(bea.Id, "chess")], this._cancellation);

        // Act
        await this._store.Remove(Ada.Id, this._cancellation);

        // Assert
        Assert.Equal(
            [new Standing(bea.Id, "chess")],
            await new EfPlayerStore(this._database.NewContext()).Leaders("chess", 10, this._cancellation)
        );
    }

    [Fact]
    public async Task Merge_WhenAnotherPlayerIsMergedIn_TheirLoginsSessionsAndStandingsAreTheOnesAndTheyAreGone()
    {
        // Arrange - Bea stands at chess, as Ada does, and at go, as Ada does not
        Login discord = new("Discord", "99");
        Player bea = new(new PlayerId("bea"), "Bea", [discord]);
        await this._store.Add(Ada, this._cancellation);
        await this._store.Add(bea, this._cancellation);
        await this._store.AddSession(bea.Id, "bea", this._cancellation);
        await this._store.Keep(
            [
                new Standing(Ada.Id, "chess") { Elo = 1600, Won = 1 },
                new Standing(bea.Id, "chess") { Elo = 1300, Lost = 2 },
                new Standing(bea.Id, "go") { Elo = 1550, Drawn = 1 },
            ],
            this._cancellation
        );

        // Act
        await new EfPlayerStore(this._database.NewContext()).Merge(bea.Id, Ada.Id, this._cancellation);

        // Assert
        EfPlayerStore reader = new(this._database.NewContext());
        Assert.Equal(
            ("ada Ada Discord:99 GitHub:1234 Google:abcd", "ada", (string?)null),
            (
                Described(await reader.ByLogin(discord, this._cancellation)),
                (await reader.BySession("bea", this._cancellation))?.Id.Value,
                Described(await reader.ById(bea.Id, this._cancellation))
            )
        );
        Assert.Equal(
            [
                new Standing(Ada.Id, "chess") { Elo = 1400, Won = 1, Lost = 2 },
                new Standing(Ada.Id, "go") { Elo = 1550, Drawn = 1 },
            ],
            [
                .. await reader.Leaders("chess", 10, this._cancellation),
                .. await reader.Leaders("go", 10, this._cancellation),
            ]
        );
    }

    [Theory]
    [InlineData("hash", true)]
    [InlineData("other", false)]
    public async Task BySession_WhenASessionWasAdded_ReadsItsPlayerBackByItsHashOnly(string hash, bool isFound)
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);
        await this._store.AddSession(Ada.Id, "hash", this._cancellation);

        // Act
        Player? found = await new EfPlayerStore(this._database.NewContext()).BySession(hash, this._cancellation);

        // Assert
        Assert.Equal(isFound ? Described(Ada) : null, Described(found));
    }

    [Fact]
    public async Task RemoveSession_WhenOneOfTwo_LetsThatOneGo()
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);
        await this._store.AddSession(Ada.Id, "here", this._cancellation);
        await this._store.AddSession(Ada.Id, "there", this._cancellation);

        // Act
        await this._store.RemoveSession("here", this._cancellation);

        // Assert
        EfPlayerStore reader = new(this._database.NewContext());
        Assert.Equal(
            (null, Described(Ada)),
            (
                Described(await reader.BySession("here", this._cancellation)),
                Described(await reader.BySession("there", this._cancellation))
            )
        );
    }

    [Fact]
    public async Task Remove_WhenAPlayerHasSessions_ForgetsThemTheirLoginsAndSessionsAndNoOneElse()
    {
        // Arrange
        Player bea = new(new PlayerId("bea"), "Bea", [new Login("GitHub", "5678")]);
        await this._store.Add(Ada, this._cancellation);
        await this._store.Add(bea, this._cancellation);
        await this._store.AddSession(Ada.Id, "ada", this._cancellation);
        await this._store.AddSession(bea.Id, "bea", this._cancellation);

        // Act
        await this._store.Remove(Ada.Id, this._cancellation);

        // Assert
        EfPlayerStore reader = new(this._database.NewContext());
        Assert.Equal(
            (null, null, Described(bea)),
            (
                Described(await reader.ByLogin(GitHub, this._cancellation)),
                Described(await reader.BySession("ada", this._cancellation)),
                Described(await reader.BySession("bea", this._cancellation))
            )
        );
    }

    [Theory]
    [InlineData("ada", "ada Ada GitHub:1234 Google:abcd")]
    [InlineData("gone", null)]
    public async Task ById_WhenAPlayerWasAdded_ReadsThemBackByTheirIdOnly(string id, string? expected)
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);

        // Act
        Player? found = await new EfPlayerStore(this._database.NewContext()).ById(new PlayerId(id), this._cancellation);

        // Assert
        Assert.Equal(expected, Described(found));
    }

    [Fact]
    public async Task Rename_WhenThePlayerIsKnown_NamesThemAnew()
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);

        // Act
        await this._store.Rename(Ada.Id, "Countess", this._cancellation);

        // Assert
        Player? found = await new EfPlayerStore(this._database.NewContext()).ById(Ada.Id, this._cancellation);
        Assert.Equal("Countess", found?.Name);
    }

    [Fact]
    public async Task AddLogin_WhenTheLoginIsNoOnes_MakesItThePlayersToo()
    {
        // Arrange
        Login discord = new("Discord", "99");
        await this._store.Add(Ada, this._cancellation);

        // Act
        await this._store.AddLogin(Ada.Id, discord, this._cancellation);

        // Assert
        Player? found = await new EfPlayerStore(this._database.NewContext()).ByLogin(discord, this._cancellation);
        Assert.Equal("ada Ada Discord:99 GitHub:1234 Google:abcd", Described(found));
    }

    [Fact]
    public async Task RemoveLogin_WhenThePlayerHasIt_LetsItGoAlone()
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);

        // Act
        await this._store.RemoveLogin(Ada.Id, Google, this._cancellation);

        // Assert
        Player? found = await new EfPlayerStore(this._database.NewContext()).ById(Ada.Id, this._cancellation);
        Assert.Equal("ada Ada GitHub:1234", Described(found));
    }

    [Fact]
    public async Task LastSignedIn_WhenSomePlayersSignedInSinceItWasKept_ReadsBackTheirsAlone()
    {
        // Arrange
        Player bea = new(new PlayerId("bea"), "Bea", [new Login("GitHub", "5678")]);
        DateTimeOffset time = new(2026, 11, 4, 12, 0, 0, TimeSpan.FromHours(1));
        await this._store.Add(Ada, this._cancellation);
        await this._store.Add(bea, this._cancellation);
        await this._store.MarkSignedIn(Ada.Id, time, this._cancellation);

        // Act
        ImmutableDictionary<PlayerId, DateTimeOffset> signedIn = await new EfPlayerStore(this._database.NewContext())
            .LastSignedIn([Ada.Id, bea.Id, new PlayerId("gone"), Ada.Id], this._cancellation);

        // Assert
        Assert.Equal([KeyValuePair.Create(Ada.Id, time)], signedIn);
    }

    /// <summary>"ada Ada GitHub:1234 Google:abcd": the player, their logins in order of provider.</summary>
    private static string? Described(Player? player) =>
        player is null
            ? null
            : string.Join(
                ' ',
                [
                    player.Id.Value,
                    player.Name,
                    .. player.Logins.OrderBy(login => login.Provider, StringComparer.Ordinal)
                        .Select(login => $"{login.Provider}:{login.Key}"),
                ]
            );
}
