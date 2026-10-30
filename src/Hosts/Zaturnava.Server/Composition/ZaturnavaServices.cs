using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Everything the server is made of, in one place: which rulesets and bots it hosts, where games
/// are stored, and where its page is served from. Each is configuration or one line here.
/// </summary>
internal static class ZaturnavaServices
{
    /// <summary>The configuration key for the most moves bots may make in a row before a person must.</summary>
    public const string MaximumBotStepsKey = "Games:MaximumBotSteps";
    public const string AwayGraceKey = "Games:AwayGraceSeconds";

    /// <summary>
    /// The configuration key for the folder of scenarios to offer (<see cref="ScenarioFolder"/>), from the
    /// content root; none are offered if it is not set, as only development sets it.
    /// </summary>
    public const string ScenariosKey = "Scenarios:Folder";

    /// <summary>
    /// The configuration section giving, in seconds, how long each timed decision may take, by the
    /// ruleset's name for its timer; <c>Default</c> for any it does not name.
    /// </summary>
    public const string TimersKey = "Games:Timers";

    private const string DefaultTimerKey = "Default";

    public static IServiceCollection AddZaturnava(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddGameStorage(configuration)
            .AddPageHosting(configuration)
            .AddSingleton<IRulesetLocating>(new RulesetCatalog([new Chaturaji2027Rules()]))
            .AddSingleton<IBotLocating>(Bots())
            .AddSingleton(Scenarios(configuration))
            .AddSingleton<IGameNotifying, SilentNotifier>()
            .AddSingleton<IDiceRolling>(new RandomDice(Random.Shared))
            .AddSingleton(new DecisionClock(TimeProvider.System))
            .AddSingleton(Settings(configuration))
            .AddScoped<GameService>()
            .AddProblemDetails()
            .AddExceptionHandler<RefusalHandler>();

    private static IScenarioLocating Scenarios(IConfiguration configuration) =>
        configuration[ScenariosKey] is { } folder
            ? new ScenarioFolder(Path.GetFullPath(folder, ContentRoot(configuration)))
            : ScenarioCatalog.None;

    private static string ContentRoot(IConfiguration configuration) =>
        configuration[HostDefaults.ContentRootKey] ?? Environment.CurrentDirectory;

    private static BotCatalog Bots() =>
        new(
            [
                new AthenaBot(Random.Shared),
                new SageBot(Random.Shared),
                new ArgusBot(Random.Shared),
                new MidasBot(Random.Shared),
                new SolomonBot(Random.Shared),
                new RandomBot(Random.Shared),
            ],
            BotRatings.Rated
        );

    private static GameServiceSettings Settings(IConfiguration configuration)
    {
        IConfigurationSection timers = configuration.GetSection(TimersKey);
        double defaultSeconds =
            timers.GetValue(DefaultTimerKey, GameServiceSettings.DefaultTimerLength.TotalSeconds);
        return new GameServiceSettings(
            configuration.GetValue(MaximumBotStepsKey, GameServiceSettings.DefaultMaximumBotSteps),
            TimeSpan.FromSeconds(defaultSeconds),
            timers.GetChildren()
                .Where(timer => timer.Key != DefaultTimerKey)
                .ToImmutableDictionary(timer => timer.Key, timer => TimeSpan.FromSeconds(timer.Get<double>()))
        )
        {
            AwayGrace = TimeSpan.FromSeconds(
                configuration.GetValue(AwayGraceKey, GameServiceSettings.DefaultAwayGrace.TotalSeconds)
            ),
        };
    }
}
