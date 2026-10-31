using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore;
using Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Where games, and what is said at their tables, are kept, chosen by configuration: <c>InMemory</c> (the
/// default; lost on restart), <c>Sqlite</c>, or <c>SqlServer</c>. The database ones take the
/// <c>Games</c> connection string, and create their tables on start if there are none (a database made before
/// the chat has no table for it, and must be made again).
/// </summary>
internal static class GameStorage
{
    public const string ProviderKey = "Storage:Provider";
    public const string ConnectionName = "Games";
    public const string InMemory = "InMemory";
    public const string Sqlite = "Sqlite";
    public const string SqlServer = "SqlServer";

    /// <exception cref="InvalidOperationException">
    /// The provider is unknown, or (once a database is opened) its connection string is missing.
    /// </exception>
    public static IServiceCollection AddGameStorage(this IServiceCollection services, IConfiguration configuration) =>
        AddProvider(services, configuration, configuration[ProviderKey] ?? InMemory);

    private static IServiceCollection AddProvider(
        IServiceCollection services,
        IConfiguration configuration,
        string provider
    ) =>
        provider switch
        {
            InMemory => services
                .AddSingleton<IGamePersisting, InMemoryGameStore>()
                .AddSingleton<IChatPersisting, InMemoryChatStore>(),
            Sqlite => AddDatabase(services, options => options.UseSqlite(Connection(configuration))),
            SqlServer => AddDatabase(services, options => options.UseSqlServer(Connection(configuration))),
            _ => throw new InvalidOperationException(
                $"There is no '{provider}' storage; {ProviderKey} must be {InMemory}, {Sqlite} or {SqlServer}."
            ),
        };

    private static IServiceCollection AddDatabase(IServiceCollection services, Action<DbContextOptionsBuilder> use) =>
        services
            .AddDbContext<GameDbContext>(use)
            .AddScoped<IGamePersisting, EfGameStore>()
            .AddScoped<IChatPersisting, EfChatStore>()
            .AddHostedService<SchemaCreator>();

    private static string Connection(IConfiguration configuration) =>
        configuration.GetConnectionString(ConnectionName)
        ?? throw new InvalidOperationException($"The '{ConnectionName}' connection string is missing.");
}
