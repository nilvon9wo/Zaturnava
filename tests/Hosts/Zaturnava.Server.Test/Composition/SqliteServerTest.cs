using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Composition;

/// <summary>The server on a database: it creates its tables on start, then keeps games in them.</summary>
public sealed class SqliteServerTest : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"zaturnava-{Guid.NewGuid():N}.db");
    private readonly ZaturnavaServer _server;

    public SqliteServerTest() => this._server = new SqliteServer($"Data Source={this._file};Pooling=False");

    public void Dispose()
    {
        this._server.Dispose();
        File.Delete(this._file);
    }

    [Fact]
    public async Task Get_WhenAGameWasCreated_ReadsItBackFromTheDatabase()
    {
        // Arrange
        CancellationToken cancellation = TestContext.Current.CancellationToken;
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat);
        GameCreatedDto created = await this._server.GameClient().Create(request, cancellation);

        // Act
        GameViewDto view = await this._server.GameClient().Get(created.GameId, cancellation);

        // Assert
        Assert.Equal((created.GameId, 0), (view.Id, view.Version));
    }
}

/// <summary>The server with its storage replaced by a SQLite database, as configuration would.</summary>
file sealed class SqliteServer(string connection) : ZaturnavaServer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services => services.AddGameStorage(this.Configuration()));

    private IConfiguration Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                [
                    KeyValuePair.Create(GameStorage.ProviderKey, (string?)GameStorage.Sqlite),
                    KeyValuePair.Create("ConnectionStrings:" + GameStorage.ConnectionName, (string?)connection),
                ]
            )
            .Build();
}
