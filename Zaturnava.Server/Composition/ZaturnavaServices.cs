using System.Collections.Immutable;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;
using Net.NowhereAtAll.Zaturnava.Variants.Classic;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Everything the server is made of, in one place: which rulesets and bots it hosts, where games
/// are stored, and who may call it from a browser. Each is configuration or one line here.
/// </summary>
internal static class ZaturnavaServices
{
    /// <summary>The configuration key for the most moves bots may make in a row before a person must.</summary>
    public const string MaximumBotStepsKey = "Games:MaximumBotSteps";

    /// <summary>
    /// The configuration section of the browser origins (the itch.io page) allowed to call, each under a
    /// name of its own, so that another settings file adds an origin rather than replacing one by index.
    /// </summary>
    public const string CorsOriginsKey = "Cors:Origins";

    public static IServiceCollection AddZaturnava(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddGameStorage(configuration)
            .AddSingleton<IRulesetLocating>(new RulesetCatalog([new ClassicRules()]))
            .AddSingleton<IBotLocating>(Bots())
            .AddSingleton<IGameNotifying, SilentNotifier>()
            .AddSingleton(Settings(configuration))
            .AddScoped<GameService>()
            .AddProblemDetails()
            .AddExceptionHandler<RefusalHandler>()
            .AddCors(cors => cors.AddDefaultPolicy(policy => AllowOrigins(policy, configuration)));

    private static BotCatalog Bots() =>
        new(ImmutableDictionary<string, IDeciding>.Empty.Add(RandomBot.BotId, new RandomBot(Random.Shared)));

    private static GameServiceSettings Settings(IConfiguration configuration) =>
        new(configuration.GetValue(MaximumBotStepsKey, GameServiceSettings.DefaultMaximumBotSteps));

    private static void AllowOrigins(CorsPolicyBuilder policy, IConfiguration configuration) =>
        _ = policy
            .WithOrigins([.. Origins(configuration)])
            .AllowAnyHeader()
            .AllowAnyMethod();

    private static IEnumerable<string> Origins(IConfiguration configuration) =>
        configuration.GetSection(CorsOriginsKey).GetChildren().Select(origin => origin.Value).OfType<string>();
}
