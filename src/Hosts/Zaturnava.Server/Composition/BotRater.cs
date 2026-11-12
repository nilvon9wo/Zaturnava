using Net.NowhereAtAll.Zaturnava.Application.Bots;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Rates the server's bots in the background while it runs (<see cref="BotRatingService"/>): first, by whatever games
/// were kept before, so that a run stopped, however and whenever, still counts; then, if
/// <see cref="Pace.IsOn"/>, one game after another, each kept as it ends and the ratings fitted anew, with a pause
/// between, so that people's games go first. A game that fails, or is cut short as the server stops, is let go, and
/// the next goes on, until the server stops.
/// </summary>
internal sealed partial class BotRater(IServiceScopeFactory scopes, BotRater.Pace pace, ILogger<BotRater> logger)
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopes = scopes;
    private readonly Pace _pace = pace;
    private readonly ILogger<BotRater> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await this.Attempt(service => service.Apply(stoppingToken));
        while (this._pace.IsOn)
        {
            // the pause ends it, once the server stops
            await Task.Delay(this._pace.Between, stoppingToken);
            await this.Attempt(async service =>
            {
                _ = await service.PlayNext(stoppingToken);
                _ = await service.Apply(stoppingToken);
            });
        }
    }

    private async Task Attempt(Func<BotRatingService, Task> rate)
    {
        try
        {
            using IServiceScope scope = this._scopes.CreateScope();
            await rate(scope.ServiceProvider.GetRequiredService<BotRatingService>());
        }
        catch (Exception exception) // whatever the bots or the store throw, the next game goes on
        {
            LogFailed(this._logger, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rating the bots failed this time; it goes on.")]
    private static partial void LogFailed(ILogger logger, Exception exception);

    /// <summary>Whether the bots play each other to be rated, and how long to wait between their games.</summary>
    public sealed record Pace(bool IsOn, TimeSpan Between)
    {
        /// <summary>The pause between games, unless configured (Proposed).</summary>
        public static readonly TimeSpan DefaultBetween = TimeSpan.FromSeconds(10);
    }
}
