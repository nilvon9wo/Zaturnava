using Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Brings the games' schema up to date when the server starts (<see cref="GameSchema"/>): a free database is
/// handed over empty, and nobody runs migrations by hand on it.
/// </summary>
internal sealed class SchemaMigrator(IServiceScopeFactory scopes) : IHostedService
{
    private readonly IServiceScopeFactory _scopes = scopes;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = this._scopes.CreateScope();
        GameDbContext context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        await context.Database.MigrateGamesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
