using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Counts the games over in their players' standings (<see cref="StandingService.RateFinished"/>), looking every
/// <see cref="StandingSweeper.Interval"/> while the server runs: one look at a time, the next timed from the end of
/// the last, so that no game is counted twice.
/// </summary>
internal sealed class StandingSweeper : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopes;
    private readonly Interval _interval;
    private readonly Timer _timer;

    public StandingSweeper(IServiceScopeFactory scopes, Interval interval)
    {
        this._scopes = scopes;
        this._interval = interval;
        this._timer = new Timer(this.Tick);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = this._timer.Change(this._interval.Every, Timeout.InfiniteTimeSpan);
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
        try
        {
            using IServiceScope scope = this._scopes.CreateScope();
            _ = await scope.ServiceProvider.GetRequiredService<StandingService>().RateFinished(CancellationToken.None);
        }
        finally
        {
            _ = this._timer.Change(this._interval.Every, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>How often games over are looked for.</summary>
    public sealed record Interval(TimeSpan Every)
    {
        public static readonly TimeSpan Default = TimeSpan.FromMinutes(1);
    }
}
