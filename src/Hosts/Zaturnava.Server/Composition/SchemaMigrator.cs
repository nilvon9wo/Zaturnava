using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Brings the games' schema up to date when the server starts (<see cref="GameSchema"/>): a free database is
/// handed over empty, and nobody runs migrations by hand on it. Then works out, once, what a lobby lists each game
/// saved before that was kept by (<see cref="GameService.Summarize"/>), and keeps in each game dealt before games
/// kept their deals what it was dealt (<see cref="GameService.KeepDeals"/>).
/// </summary>
internal sealed class SchemaMigrator(IServiceScopeFactory scopes) : IHostedService
{
    private readonly IServiceScopeFactory _scopes = scopes;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = this._scopes.CreateScope();
        GameDbContext context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        await context.Database.MigrateGamesAsync(cancellationToken);
        GameService games = scope.ServiceProvider.GetRequiredService<GameService>();
        await games.Summarize(cancellationToken);
        _ = await games.KeepDeals(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
