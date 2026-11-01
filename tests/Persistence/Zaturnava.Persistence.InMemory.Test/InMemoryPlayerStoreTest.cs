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
    public async Task Changes_WhenThePlayerIsKnown_RenameAndLinkAndUnlinkThem()
    {
        // Arrange
        Login google = new("Google", "abcd");
        await this._store.Add(Ada, this._cancellation);

        // Act
        await this._store.Rename(Ada.Id, "Countess", this._cancellation);
        await this._store.AddLogin(Ada.Id, google, this._cancellation);
        await this._store.RemoveLogin(Ada.Id, GitHub, this._cancellation);

        // Assert
        Player? found = await this._store.ById(Ada.Id, this._cancellation);
        Assert.Equal(("Countess", google), (found?.Name, found?.Logins.Single()));
    }

    [Fact]
    public async Task SetLinks_WhenThePlayerIsKnown_ShowsThoseInstead()
    {
        // Arrange
        await this._store.Add(Ada with { Links = ["https://old.example"] }, this._cancellation);

        // Act
        await this._store.SetLinks(Ada.Id, ["https://new.example"], this._cancellation);

        // Assert
        Player? found = await this._store.ById(Ada.Id, this._cancellation);
        Assert.Equal(["https://new.example"], found?.Links ?? []);
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
