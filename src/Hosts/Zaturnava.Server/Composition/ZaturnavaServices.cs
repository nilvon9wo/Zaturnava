using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Chat;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using Net.NowhereAtAll.Zaturnava.Games.GrandLegions;
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
    /// The configuration key for how long, in seconds, a game waits for people to take its open seats before the
    /// first of those seated may fill them with bots.
    /// </summary>
    public const string FillWaitKey = "Games:FillWaitSeconds";

    /// <summary>
    /// The configuration key for how long, in seconds, after the first to arrive may have a bot play for a player
    /// away, any other player may.
    /// </summary>
    public const string ReplaceWaitKey = "Games:ReplaceWaitSeconds";

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

    /// <summary>
    /// The configuration keys for how long, in seconds, no player at a table may be heard from before what was
    /// said there is let go, and how often that is looked for (<see cref="ChatSettings"/>).
    /// </summary>
    public const string ChatLeftAfterKey = "Chat:LeftAfterSeconds";
    public const string ChatSweepKey = "Chat:SweepSeconds";

    /// <summary>
    /// The configuration keys for how long, in seconds, a game not over with a guest at it is kept once nothing more
    /// happens; one waiting for a signed-in player who has not signed in for as long; and how often such games are
    /// looked for (<see cref="GameSweeper"/>).
    /// </summary>
    public const string GuestGamesKeptKey = "Games:GuestGamesKeptSeconds";
    public const string AwaitedGamesKeptKey = "Games:AwaitedGamesKeptSeconds";
    public const string GameSweepKey = "Games:SweepSeconds";

    /// <summary>
    /// The configuration key for how often, in seconds, games over are counted in their players' standings
    /// (<see cref="StandingSweeper"/>).
    /// </summary>
    public const string StandingSweepKey = "Standings:SweepSeconds";

    /// <summary>
    /// The configuration section giving the limits of the safety nets that end a game on points
    /// (<see cref="SafetyNets"/>), by their names; the rules' own for any not given.
    /// </summary>
    public const string SafetyNetsKey = "Games:SafetyNets";

    /// <summary>
    /// The configuration keys for whether the server's bots play each other to be rated, how long to pause, in
    /// seconds, between their games, the most decisions a game of theirs may take, and how many games each pair must
    /// play at a ruleset before its ratings are fitted anew (<see cref="BotRater"/>, <see cref="BotRatingSettings"/>).
    /// </summary>
    public const string BotRatingOnKey = "BotRating:IsOn";
    public const string BotRatingPauseKey = "BotRating:PauseSeconds";
    public const string BotRatingStepsKey = "BotRating:MaximumSteps";
    public const string BotRatingGamesKey = "BotRating:MinimumGames";

    private const string DefaultTimerKey = "Default";

    public static IServiceCollection AddZaturnava(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddGameStorage(configuration)
            .AddPageHosting(configuration)
            .AddSignIn(configuration)
            .AddRequestLimits(configuration)
            .AddSingleton<IRulesetLocating>(Rulesets(SafetyNetsOf(configuration)))
            .AddSingleton(Bots())
            .AddSingleton<IBotLocating>(provider => provider.GetRequiredService<BotCatalog>())
            .AddSingleton(Scenarios(configuration))
            .AddSingleton<IGameNotifying, SilentNotifier>()
            .AddSingleton<IDiceRolling>(new RandomDice(Random.Shared))
            .AddSingleton(new DecisionClock(TimeProvider.System))
            .AddSingleton<GameGate>()
            .AddSingleton(Settings(configuration))
            .AddScoped<GameService>()
            .AddScoped<ChatService>()
            .AddScoped<PlayerService>()
            .AddSingleton(TimeProvider.System)
            .AddSingleton(new LinkCodes(TimeProvider.System))
            .AddSingleton(new GuestAliases(TimeProvider.System))
            .AddSingleton(ChatSettingsOf(configuration))
            .AddSingleton<ChatTables>()
            .AddHostedService<ChatSweeper>()
            .AddSingleton(
                new GameSweeper.Interval(
                    TimeSpan.FromSeconds(
                        configuration.GetValue(GameSweepKey, GameSweeper.Interval.Default.TotalSeconds)
                    )
                )
            )
            .AddHostedService<GameSweeper>()
            .AddScoped<StandingService>()
            .AddSingleton(
                new StandingSweeper.Interval(
                    TimeSpan.FromSeconds(
                        configuration.GetValue(StandingSweepKey, StandingSweeper.Interval.Default.TotalSeconds)
                    )
                )
            )
            .AddHostedService<StandingSweeper>()
            .AddSingleton(BotRatingOf(configuration))
            .AddScoped<BotRatingService>()
            .AddSingleton(
                new BotRater.Pace(
                    configuration.GetValue(BotRatingOnKey, false),
                    TimeSpan.FromSeconds(
                        configuration.GetValue(BotRatingPauseKey, BotRater.Pace.DefaultBetween.TotalSeconds)
                    )
                )
            )
            .AddHostedService<BotRater>()
            .AddProblemDetails()
            .AddExceptionHandler<RefusalHandler>();

    private static RulesetCatalog Rulesets(SafetyNets nets) =>
        new([new Chaturaji2027Rules(nets), .. GrandLegionsRules.QuickWith(nets)]);

    /// <summary>The safety nets' limits as configured, each the rules' own where it is not.</summary>
    private static SafetyNets SafetyNetsOf(IConfiguration configuration)
    {
        IConfigurationSection nets = configuration.GetSection(SafetyNetsKey);
        return new SafetyNets(
            nets.GetValue(nameof(SafetyNets.IdleRounds), SafetyNets.Default.IdleRounds),
            nets.GetValue(nameof(SafetyNets.QuietRounds), SafetyNets.Default.QuietRounds),
            nets.GetValue(nameof(SafetyNets.RepeatRounds), SafetyNets.Default.RepeatRounds)
        );
    }

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
                new PythiaBot(Random.Shared),
                new TacticianBot(Random.Shared),
                new SageBot(Random.Shared),
                new ArgusBot(Random.Shared),
                new MidasBot(Random.Shared),
                new SolomonBot(Random.Shared),
                NoviceBot.Icarus(Random.Shared),
                NoviceBot.Echo(Random.Shared),
                new RandomBot(Random.Shared),
            ],
            BotRatings.Rated
        );

    /// <summary>How the bots are rated, as configured, anchored by Random's rating.</summary>
    private static BotRatingSettings BotRatingOf(IConfiguration configuration)
    {
        BotRatingSettings defaults = new(RandomBot.BotId);
        return defaults with
        {
            MaximumSteps = configuration.GetValue(BotRatingStepsKey, defaults.MaximumSteps),
            MinimumGames = configuration.GetValue(BotRatingGamesKey, defaults.MinimumGames),
        };
    }

    private static ChatSettings ChatSettingsOf(IConfiguration configuration) =>
        new(
            TimeSpan.FromSeconds(
                configuration.GetValue(ChatLeftAfterKey, ChatSettings.DefaultLeftAfter.TotalSeconds)
            ),
            TimeSpan.FromSeconds(configuration.GetValue(ChatSweepKey, ChatSettings.DefaultSweepInterval.TotalSeconds))
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
            FillWait = TimeSpan.FromSeconds(
                configuration.GetValue(FillWaitKey, GameServiceSettings.DefaultFillWait.TotalSeconds)
            ),
            ReplaceWait = TimeSpan.FromSeconds(
                configuration.GetValue(ReplaceWaitKey, GameServiceSettings.DefaultReplaceWait.TotalSeconds)
            ),
            GuestGamesKept = TimeSpan.FromSeconds(
                configuration.GetValue(GuestGamesKeptKey, GameServiceSettings.DefaultGuestGamesKept.TotalSeconds)
            ),
            AwaitedGamesKept = TimeSpan.FromSeconds(
                configuration.GetValue(AwaitedGamesKeptKey, GameServiceSettings.DefaultAwaitedGamesKept.TotalSeconds)
            ),
        };
    }
}
