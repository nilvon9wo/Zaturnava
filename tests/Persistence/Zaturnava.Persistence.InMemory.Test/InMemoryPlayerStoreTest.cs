using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Persistence.InMemory.Test;

public sealed class InMemoryPlayerStoreTest
{
    private static readonly Login GitHub = new("GitHub", "1234");
    private static readonly Player Ada = new(new PlayerId("ada"), "Ada", [GitHub]);

    private readonly InMemoryPlayerStore _store = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("GitHub", "1234", true)]
    [InlineData("GitHub", "5678", false)]
    [InlineData("Google", "1234", false)]
    public async Task ByLogin_WhenAPlayerWasAdded_FindsThemByTheirLoginOnly(string provider, string key, bool isFound)
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);

        // Act
        Player? found = await this._store.ByLogin(new Login(provider, key), this._cancellation);

        // Assert
        Assert.Equal(isFound, found == Ada);
    }

    [Fact]
    public async Task Names_WhenAskedOfKnownAndUnknownPlayers_NamesTheKnownOnes()
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);

        // Act
        ImmutableDictionary<PlayerId, string> names =
            await this._store.Names([Ada.Id, new PlayerId("gone"), Ada.Id], this._cancellation);

        // Assert
        Assert.Equal([KeyValuePair.Create(Ada.Id, "Ada")], names);
    }

    [Theory]
    [InlineData("hash", true)]
    [InlineData("other", false)]
    public async Task BySession_WhenASessionWasAdded_FindsItsPlayerByItsHashOnly(string hash, bool isFound)
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);
        await this._store.AddSession(Ada.Id, "hash", this._cancellation);

        // Act
        Player? found = await this._store.BySession(hash, this._cancellation);

        // Assert
        Assert.Equal(isFound, found == Ada);
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
        Assert.Equal(
            (null, Ada),
            (await this._store.BySession("here", this._cancellation),
                await this._store.BySession("there", this._cancellation))
        );
    }

    [Theory]
    [InlineData("ada", true)]
    [InlineData("gone", false)]
    public async Task ById_WhenAPlayerWasAdded_FindsThemByTheirIdOnly(string id, bool isFound)
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);

        // Act
        Player? found = await this._store.ById(new PlayerId(id), this._cancellation);

        // Assert
        Assert.Equal(isFound, found == Ada);
    }

    [Fact]
    public async Task Changes_WhenThePlayerIsKnown_RenameAndLinkAndUnlinkThemAndKeepTheirSettings()
    {
        // Arrange
        Login google = new("Google", "abcd");
        await this._store.Add(Ada, this._cancellation);

        // Act
        await this._store.Rename(Ada.Id, "Countess", this._cancellation);
        await this._store.AddLogin(Ada.Id, google, this._cancellation);
        await this._store.RemoveLogin(Ada.Id, GitHub, this._cancellation);
        await this._store.KeepPreferences(Ada.Id, Player.NoPreferences.Add("theme", "light"), this._cancellation);

        // Assert
        Player? found = await this._store.ById(Ada.Id, this._cancellation);
        Assert.Equal(
            ("Countess", google, "light"),
            (found?.Name, found?.Logins.Single(), found?.Preferences.GetValueOrDefault("theme"))
        );
    }

    [Fact]
    public async Task Rename_WhenThePlayerIsUnknown_ChangesNothing()
    {
        // Arrange - nothing beyond the empty store

        // Act
        await this._store.Rename(Ada.Id, "Countess", this._cancellation);

        // Assert
        Assert.Null(await this._store.ById(Ada.Id, this._cancellation));
    }

    [Fact]
    public async Task LastSignedIn_WhenSomePlayersSignedIn_ReadsBackTheirsAlone()
    {
        // Arrange
        DateTimeOffset time = DateTimeOffset.UnixEpoch;
        await this._store.Add(Ada, this._cancellation);
        await this._store.MarkSignedIn(Ada.Id, time, this._cancellation);

        // Act
        ImmutableDictionary<PlayerId, DateTimeOffset> signedIn =
            await this._store.LastSignedIn([Ada.Id, new PlayerId("bea"), Ada.Id], this._cancellation);

        // Assert
        Assert.Equal([KeyValuePair.Create(Ada.Id, time)], signedIn);
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
        await this._store.AddSession(Ada.Id, "ada", this._cancellation);
        await this._store.Keep(
            [
                new Standing(Ada.Id, "chess") { Elo = 1600, Won = 1 },
                new Standing(bea.Id, "chess") { Elo = 1300, Lost = 2 },
                new Standing(bea.Id, "go") { Elo = 1550, Drawn = 1 },
            ],
            this._cancellation
        );

        // Act
        await this._store.Merge(bea.Id, Ada.Id, this._cancellation);

        // Assert
        Assert.Equal(
            (Ada.Id, Ada.Id, Ada.Id, null),
            (
                (await this._store.ByLogin(discord, this._cancellation))?.Id,
                (await this._store.BySession("bea", this._cancellation))?.Id,
                (await this._store.BySession("ada", this._cancellation))?.Id,
                await this._store.ById(bea.Id, this._cancellation)
            )
        );
        Assert.Equal(
            [
                new Standing(Ada.Id, "chess") { Elo = 1400, Won = 1, Lost = 2 },
                new Standing(Ada.Id, "go") { Elo = 1550, Drawn = 1 },
            ],
            [
                .. await this._store.Leaders("chess", 10, this._cancellation),
                .. await this._store.Leaders("go", 10, this._cancellation),
            ]
        );
    }

    [Fact]
    public async Task Remove_WhenThePlayerSignedIn_ForgetsWhen()
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);
        await this._store.MarkSignedIn(Ada.Id, DateTimeOffset.UnixEpoch, this._cancellation);

        // Act
        await this._store.Remove(Ada.Id, this._cancellation);

        // Assert
        Assert.Empty(await this._store.LastSignedIn([Ada.Id], this._cancellation));
    }

    [Fact]
    public async Task Remove_WhenThePlayerHasNoSession_ForgetsThemStill()
    {
        // Arrange
        await this._store.Add(Ada, this._cancellation);

        // Act
        await this._store.Remove(Ada.Id, this._cancellation);

        // Assert
        Assert.Null(await this._store.ByLogin(GitHub, this._cancellation));
    }

    [Fact]
    public async Task Standings_WhenKeptAndKeptAgain_ReadBackTheLatestOfTheGameAskedFor()
    {
        // Arrange
        await this._store.Keep(
            [new Standing(Ada.Id, "chess") { Elo = 1400 }, new Standing(Ada.Id, "go") { Won = 1 }],
            this._cancellation
        );
        await this._store.Keep([new Standing(Ada.Id, "chess") { Elo = 1520, Won = 2 }], this._cancellation);

        // Act
        ImmutableDictionary<PlayerId, Standing> standings =
            await this._store.Standings("chess", [Ada.Id, new PlayerId("bea"), Ada.Id], this._cancellation);

        // Assert
        Assert.Equal([KeyValuePair.Create(Ada.Id, new Standing(Ada.Id, "chess") { Elo = 1520, Won = 2 })], standings);
    }

    [Fact]
    public async Task Leaders_WhenPlayersStandAtSeveralGames_ListTheBestOfTheGameByEloThenExperienceThenId()
    {
        // Arrange
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
        ImmutableList<Standing> leaders = await this._store.Leaders("chess", 4, this._cancellation);

        // Assert
        Assert.Equal("d,b,a,c", string.Join(",", leaders.Select(leader => leader.Player.Value)));
    }

    [Fact]
    public async Task LeadersAgainstBots_WhenSomeHaveFacedBotsAlone_ListThemByWinsThenFewestLossesThenId()
    {
        // Arrange - "e" has never faced bots alone; "f" stands at another game
        await this._store.Keep(
            [
                new Standing(new PlayerId("c"), "chess") { WonAgainstBots = 2, LostAgainstBots = 1 },
                new Standing(new PlayerId("b"), "chess") { WonAgainstBots = 2, LostAgainstBots = 1 },
                new Standing(new PlayerId("a"), "chess") { WonAgainstBots = 2, LostAgainstBots = 4 },
                new Standing(new PlayerId("d"), "chess") { DrawnAgainstBots = 1 },
                new Standing(new PlayerId("e"), "chess") { Won = 9 },
                new Standing(new PlayerId("f"), "go") { WonAgainstBots = 9 },
            ],
            this._cancellation
        );

        // Act
        ImmutableList<Standing> leaders = await this._store.LeadersAgainstBots("chess", 3, this._cancellation);

        // Assert
        Assert.Equal("b,c,a", string.Join(",", leaders.Select(leader => leader.Player.Value)));
    }

    [Fact]
    public async Task BotStandings_WhenKeptAndKeptAgain_ReadBackTheLatestOfTheGameAskedFor()
    {
        // Arrange
        await this._store.KeepBots(
            [new BotStanding("sage", "chess") { Won = 1 }, new BotStanding("sage", "go") { Lost = 1 }],
            this._cancellation
        );
        await this._store.KeepBots([new BotStanding("sage", "chess") { Won = 2 }], this._cancellation);

        // Act
        ImmutableDictionary<string, BotStanding> standings =
            await this._store.BotStandings("chess", ["sage", "random", "sage"], this._cancellation);

        // Assert
        Assert.Equal([KeyValuePair.Create("sage", new BotStanding("sage", "chess") { Won = 2 })], standings);
    }

    [Fact]
    public async Task BotLeaders_WhenBotsStandAtSeveralGames_ListTheBestOfTheGameByWinsThenFewestLossesThenId()
    {
        // Arrange
        await this._store.KeepBots(
            [
                new BotStanding("c", "chess") { Won = 3, Lost = 1 },
                new BotStanding("b", "chess") { Won = 3, Lost = 1 },
                new BotStanding("a", "chess") { Won = 3, Lost = 2 },
                new BotStanding("d", "chess"),
                new BotStanding("e", "go") { Won = 9 },
            ],
            this._cancellation
        );

        // Act
        ImmutableList<BotStanding> leaders = await this._store.BotLeaders("chess", 3, this._cancellation);

        // Assert
        Assert.Equal("b,c,a", string.Join(",", leaders.Select(leader => leader.BotId)));
    }

    [Fact]
    public async Task Remove_WhenThePlayerStandsAtAGame_ForgetsWhereTheyStoodAlone()
    {
        // Arrange
        Standing bea = new(new PlayerId("bea"), "chess");
        await this._store.Add(Ada, this._cancellation);
        await this._store.Keep([new Standing(Ada.Id, "chess"), bea], this._cancellation);

        // Act
        await this._store.Remove(Ada.Id, this._cancellation);

        // Assert
        Assert.Equal([bea], await this._store.Leaders("chess", 10, this._cancellation));
    }

    [Fact]
    public async Task Remove_WhenAPlayerHasSessions_ForgetsThemAndTheirsAlone()
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
        Assert.Equal(
            (null, null, bea),
            (
                await this._store.ByLogin(GitHub, this._cancellation),
                await this._store.BySession("ada", this._cancellation),
                await this._store.BySession("bea", this._cancellation)
            )
        );
    }
}
