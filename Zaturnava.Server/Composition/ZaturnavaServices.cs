using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;
using Net.NowhereAtAll.Zaturnava.Variants.Classic;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Everything the server is made of, in one place: which rulesets and bots it hosts, where games
/// are stored, and where its page is served from. Each is configuration or one line here.
/// </summary>
internal static class ZaturnavaServices
{
    /// <summary>The configuration key for the most moves bots may make in a row before a person must.</summary>
    public const string MaximumBotStepsKey = "Games:MaximumBotSteps";

    public static IServiceCollection AddZaturnava(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddGameStorage(configuration)
            .AddPageHosting(configuration)
            .AddSingleton<IRulesetLocating>(new RulesetCatalog([new ClassicRules()]))
            .AddSingleton<IBotLocating>(Bots())
            .AddSingleton<IGameNotifying, SilentNotifier>()
            .AddSingleton<IDiceRolling>(new RandomDice(Random.Shared))
            .AddSingleton(Settings(configuration))
            .AddScoped<GameService>()
            .AddProblemDetails()
            .AddExceptionHandler<RefusalHandler>();

    private static BotCatalog Bots() => new([new MidasBot(Random.Shared), new RandomBot(Random.Shared)]);

    private static GameServiceSettings Settings(IConfiguration configuration) =>
        new(configuration.GetValue(MaximumBotStepsKey, GameServiceSettings.DefaultMaximumBotSteps));
}
