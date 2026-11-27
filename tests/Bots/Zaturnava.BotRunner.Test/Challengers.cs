using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

/// <summary>
/// A bot as ours are but for one way of playing, to play against them in the arena (<see cref="ArenaRun"/>, with
/// <c>"botAssembly": "self"</c>), so that a way earns its place in our bots by winning, not by argument (the user,
/// 2026-11-27: "we can/should have different bots with different approaches so we have proof instead of
/// speculation"). Not offered to anyone: challengers live here, among the tests.
/// </summary>
public abstract class Challenger(IBotPlaying bot, string id, string way) : IBotPlaying
{
    private readonly IBotPlaying _bot = bot;

    public BotProfile Profile { get; } = new(id, $"{bot.Profile.Name}, {way}", way, bot.Profile.UsesAi);

    public bool Knows([Required] IRulesApplying rules) => this._bot.Knows(rules);

    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._bot.Seize(request, cancellationToken);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._bot.Decide(request, cancellationToken);

    /// <summary>Sage's weights, as learned for each ruleset, hunting as <paramref name="hunting"/> says.</summary>
    protected static ImmutableDictionary<string, SageWeights> Learned(Hunting hunting) =>
        SageWeights.Learned.ToImmutableDictionary(
            weights => weights.Key,
            weights => weights.Value with { Hunting = hunting }
        );

    /// <summary>Sage's default weights, hunting as <paramref name="hunting"/> says.</summary>
    protected static SageWeights Fallback(Hunting hunting) => SageWeights.Default with { Hunting = hunting };
}

/// <summary>Sage, hunting heavy pieces before leaders, and guarding its allies' thrones.</summary>
public sealed class HuntingSage(Random random) : Challenger(
    new SageBot(random, Fallback(Hunting.OfHeavies), Learned(Hunting.OfHeavies)),
    "sage-hunting",
    "hunting heavies"
);

/// <summary>
/// Sage, a usurper: weighing a throne seized by what it controls (the user, 2026-11-27: seizing one seldom matters,
/// "but when it counts, it really can count"), so that it makes the throne an objective, as a test of guarding needs.
/// </summary>
public sealed class UsurpingSage(Random random) : Challenger(
    new SageBot(random, Fallback(Usurping), Learned(Usurping)),
    "sage-usurping",
    "seizing thrones"
)
{
    private static Hunting Usurping => Hunting.OfLeaders with { ThronesByStake = true };
}

/// <summary>
/// Sage, guarding its allies' empty thrones by what an enemy leader stepping onto one would take control of: to play
/// against <see cref="UsurpingSage"/>, beside Sage as it is.
/// </summary>
public sealed class GuardingSage(Random random) : Challenger(
    new SageBot(random, Fallback(Guarding), Learned(Guarding)),
    "sage-guarding",
    "guarding allies' thrones"
)
{
    private static Hunting Guarding => Hunting.OfLeaders with { AlliedThrones = true };
}

/// <summary>
/// Athena, her network's judgement steadied ten times as much by Sage's (2026-11-27: between quiet moves it swung by
/// more than a Rook left to be taken counted).
/// </summary>
public sealed class SteadyAthena(Random random) : Challenger(
    new AthenaBot(random) { Steadiness = 0.02 },
    "athena-steady",
    "steadier"
);

/// <summary>
/// Tactician, hunting leaders as it does, but guarding its allies' thrones too: the guard apart from the hunt for
/// heavies (2026-11-27: the two together lost 43 to 56).
/// </summary>
public sealed class GuardingTactician(Random random) : Challenger(
    new TacticianBot(
        random,
        TacticianBot.Budget,
        TacticianBot.Width,
        Fallback(Guarding),
        Learned(Guarding)
    ),
    "tactician-guarding",
    "guarding allies' thrones"
)
{
    private static Hunting Guarding => Hunting.OfLeaders with { AlliedThrones = true };
}

/// <summary>
/// Tactician, hunting heavy pieces before leaders, but counting no more than one escape of each, and that as likely
/// as the dice let it move (the user, 2026-11-27: "it only just needs one unguarded square"); its allies' thrones as
/// it does.
/// </summary>
public sealed class CorneringTactician(Random random) : Challenger(
    new TacticianBot(
        random,
        TacticianBot.Budget,
        TacticianBot.Width,
        Fallback(Cornering),
        Learned(Cornering)
    ),
    "tactician-cornering",
    "cornering heavies"
)
{
    private static Hunting Cornering => Hunting.OfHeavies with { AlliedThrones = false, Escapes = 1 };
}

/// <summary>Tactician, hunting heavy pieces before leaders, and guarding its allies' thrones.</summary>
public sealed class HuntingTactician(Random random) : Challenger(
    new TacticianBot(
        random,
        TacticianBot.Budget,
        TacticianBot.Width,
        Fallback(Hunting.OfHeavies),
        Learned(Hunting.OfHeavies)
    ),
    "tactician-hunting",
    "hunting heavies"
);
