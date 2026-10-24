using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore;
using Net.NowhereAtAll.Zaturnava.Persistence.InMemory;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Composition;

public sealed class GameStorageTest
{
    private const string Connection = "DataSource=:memory:";

    [Fact]
    public void AddGameStorage_WhenNoProviderIsConfigured_KeepsGamesInMemory()
    {
        // Arrange
        ServiceProvider services = Services([]);

        // Act
        IGamePersisting store = services.GetRequiredService<IGamePersisting>();

        // Assert
        _ = Assert.IsType<InMemoryGameStore>(store);
    }

    [Theory]
    [InlineData(GameStorage.Sqlite, "Microsoft.EntityFrameworkCore.Sqlite")]
    [InlineData(GameStorage.SqlServer, "Microsoft.EntityFrameworkCore.SqlServer")]
    public void AddGameStorage_WhenADatabaseIsConfigured_StoresGamesThere(string provider, string expected)
    {
        // Arrange
        using ServiceProvider services = Services([(GameStorage.ProviderKey, provider), (Games, Connection)]);
        using IServiceScope scope = services.CreateScope();

        // Act
        IGamePersisting store = scope.ServiceProvider.GetRequiredService<IGamePersisting>();

        // Assert
        Assert.Equal(
            (typeof(EfGameStore), expected),
            (store.GetType(), scope.ServiceProvider.GetRequiredService<GameDbContext>().Database.ProviderName)
        );
    }

    [Fact]
    public void AddGameStorage_WhenTheDatabaseHasNoConnectionString_FailsToOpenIt()
    {
        // Arrange
        using ServiceProvider services = Services([(GameStorage.ProviderKey, GameStorage.Sqlite)]);
        using IServiceScope scope = services.CreateScope();

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<GameDbContext>().Database.ProviderName
        );

        // Assert
        Assert.Contains(GameStorage.ConnectionName, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddGameStorage_WhenTheProviderIsUnknown_ThrowsInvalidOperationException()
    {
        // Arrange
        IConfiguration configuration = Configuration([(GameStorage.ProviderKey, "Floppy")]);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddGameStorage(configuration)
        );

        // Assert
        Assert.Contains("'Floppy'", exception.Message, StringComparison.Ordinal);
    }

    private static string Games => "ConnectionStrings:" + GameStorage.ConnectionName;

    private static ServiceProvider Services((string Key, string Value)[] settings) =>
        new ServiceCollection().AddGameStorage(Configuration(settings)).BuildServiceProvider();

    private static IConfiguration Configuration((string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(setting => KeyValuePair.Create(setting.Key, (string?)setting.Value)))
            .Build();
}
