using Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Creates the games' tables when the server starts, if the database has none: a free database is
/// handed over empty, and nobody runs migrations by hand on it.
/// </summary>
internal sealed class SchemaCreator(IServiceScopeFactory scopes) : IHostedService
{
    private readonly IServiceScopeFactory _scopes = scopes;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = this._scopes.CreateScope();
        GameDbContext context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        _ = await context.Database.EnsureCreatedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
