using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// What the page is wired to: which game server (wwwroot/appsettings*.json, so the server can move
/// without rebuilding the page), how often a table looks for other players' moves, and how fast bots
/// play.
/// </summary>
internal static class WebServices
{
    public const string ServerUrlKey = "Server:Url";
    public const string PollSecondsKey = "Table:PollSeconds";
    public const string BotPaceSecondsKey = "Table:BotPaceSeconds";
    public const double DefaultPollSeconds = 2;
    public const double DefaultBotPaceSeconds = 0.9;

    public static IServiceCollection AddZaturnavaWeb(
        this IServiceCollection services,
        IConfiguration configuration,
        string pageAddress
    ) =>
        services
            .AddSingleton(new HttpClient { BaseAddress = new Uri(configuration[ServerUrlKey] ?? pageAddress) })
            .AddSingleton<IGameServing, HttpGameClient>()
            .AddSingleton<Browser>()
            .AddSingleton(
                new TableSettings(
                    TimeSpan.FromSeconds(configuration.GetValue(PollSecondsKey, DefaultPollSeconds)),
                    TimeSpan.FromSeconds(configuration.GetValue(BotPaceSecondsKey, DefaultBotPaceSeconds))
                )
            );
}
