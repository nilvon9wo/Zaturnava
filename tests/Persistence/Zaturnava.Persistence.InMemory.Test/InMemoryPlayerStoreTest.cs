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
}
