using Net.NowhereAtAll.Zaturnava.Application.Chat;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Lets go of what was said at each table once all its players have left, looking every
/// <see cref="ChatSettings.SweepInterval"/> while the server runs.
/// </summary>
internal sealed class ChatSweeper : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ChatSettings _settings;
    private readonly Timer _timer;

    public ChatSweeper(IServiceScopeFactory scopes, ChatSettings settings)
    {
        this._scopes = scopes;
        this._settings = settings;
        this._timer = new Timer(this.Tick);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = this._timer.Change(this._settings.SweepInterval, this._settings.SweepInterval);
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
        _ = await scope.ServiceProvider.GetRequiredService<ChatService>().Purge(CancellationToken.None);
    }
}
