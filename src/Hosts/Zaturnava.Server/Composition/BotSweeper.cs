using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Plays on the games left waiting for one of the server's bots (<see cref="GameService.PlayIdleBots"/>), looking
/// every <see cref="BotSweeper.Interval"/> while the server runs: one look at a time, the next timed from the end of
/// the last, so that no bot is moved twice (the user's report of 2026-11-20: games no one watched waited days for a
/// bot).
/// </summary>
internal sealed class BotSweeper : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopes;
    private readonly Interval _interval;
    private readonly Timer _timer;

    public BotSweeper(IServiceScopeFactory scopes, Interval interval)
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
            _ = await scope.ServiceProvider.GetRequiredService<GameService>()
                .PlayIdleBots(this._interval.Idle, CancellationToken.None);
        }
        finally
        {
            _ = this._timer.Change(this._interval.Every, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// How often games waiting for a bot are looked for, and how long one must have waited unchanged, so that a game
    /// someone watches moves at the pace their table asks for, a step at a time, rather than all at once (Proposed).
    /// </summary>
    public sealed record Interval(TimeSpan Every, TimeSpan Idle)
    {
        public static readonly TimeSpan DefaultEvery = TimeSpan.FromSeconds(5);

        public static readonly TimeSpan DefaultIdle = TimeSpan.FromSeconds(15);
    }
}
