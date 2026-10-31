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
