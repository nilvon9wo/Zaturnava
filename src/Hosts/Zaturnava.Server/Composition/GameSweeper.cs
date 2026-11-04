using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Lets go of the games abandoned with a guest at them (<see cref="GameService.PurgeAbandoned"/>), looking every
/// <see cref="GameSweeper.Interval"/> while the server runs.
/// </summary>
internal sealed class GameSweeper : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopes;
    private readonly Interval _interval;
    private readonly Timer _timer;

    public GameSweeper(IServiceScopeFactory scopes, Interval interval)
    {
        this._scopes = scopes;
        this._interval = interval;
        this._timer = new Timer(this.Tick);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = this._timer.Change(this._interval.Every, this._interval.Every);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _ = this._timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        return Task.CompletedTask;
    }

    public void Dispose() => this._timer.Dispose();

    private void Tick(object? state) => _ = this.Sweep();

    private async Task Sweep()
    {
        using IServiceScope scope = this._scopes.CreateScope();
        _ = await scope.ServiceProvider.GetRequiredService<GameService>().PurgeAbandoned(CancellationToken.None);
    }

    /// <summary>How often abandoned games are looked for.</summary>
    public sealed record Interval(TimeSpan Every)
    {
        public static readonly TimeSpan Default = TimeSpan.FromHours(1);
    }
}
