using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// A bot for beginners, weaker than Midas and stronger than Random (the user's request of 2026-11-12, of weak bots
/// between Elo 800 and 1200, their details mine; Proposed): at each decision it plays as Midas would some of the time,
/// and as Random would the rest, its share of Midas its strength. Echo, which only now and then repeats what a
/// stronger player would do, plays as Midas one decision in four; Icarus, who flew well until he did not, one in two.
/// The bot-rating job rates each, so the picker shows how strong it turns out. Measured at 1430 and 1475, beating
/// Random nearly every game (2026-11-20), they are not the bots below 1200 the specification's D2 asks for; so three
/// more play as Midas still more seldom (<see cref="Faint"/>), kept apart from Echo and Icarus, whose games are kept.
/// </summary>
public sealed class NoviceBot : IBotPlaying
{
    public const string EchoId = "echo";
    public const string IcarusId = "icarus";

    /// <summary>The share of Echo's decisions Midas makes, as a percentage.</summary>
    public const int EchoShare = 25;

    /// <summary>The share of Icarus's decisions Midas makes, as a percentage.</summary>
    public const int IcarusShare = 50;

    private const int Whole = 100;

    private readonly MidasBot _midas;
    private readonly RandomBot _aimless;
    private readonly Random _random;
    private readonly int _share;

    private NoviceBot(BotProfile profile, int share, Random random)
    {
        this.Profile = profile;
        this._share = share;
        this._random = random;
        this._midas = new MidasBot(random);
        this._aimless = new RandomBot(random);
    }

    public BotProfile Profile { get; }

    /// <summary>Plays as Midas one decision in four, and at random the rest.</summary>
    public static NoviceBot Echo([Required] Random random) =>
        new(
            new BotProfile(
                EchoId,
                "Echo",
                "For beginners: one turn in four, grabs whatever scores the most right now, as Midas does; the rest of "
                    + "the time, plays at random. A plain algorithm: no AI.",
                UsesAi: false
            ),
            EchoShare,
            random
        );

    /// <summary>Plays as Midas one decision in two, and at random the rest.</summary>
    public static NoviceBot Icarus([Required] Random random) =>
        new(
            new BotProfile(
                IcarusId,
                "Icarus",
                "For beginners: every other turn, grabs whatever scores the most right now, as Midas does; the rest "
                    + "of the time, plays at random. A plain algorithm: no AI.",
                UsesAi: false
            ),
            IcarusShare,
            random
        );

    /// <summary>
    /// The bots for the very first games (D2: at least three below 1200, so that a game may be all easy bots; the
    /// user, 2026-11-26): Morpheus, half asleep, plays as Midas one decision in a hundred; Phaethon, who could not hold
    /// the reins, one in fifty; Tantalus, whose prize was ever just out of reach, one in twenty-five. Measured
    /// locally at one in fifty, twenty and ten (2026-11-26, 60 games a pair, Random at 1000): 1083, 1192 and 1311, so
    /// the shares halved and more, to fall between 1000 and 1200.
    /// </summary>
    public static ImmutableList<NoviceBot> Faint([Required] Random random) =>
    [
        .. FaintBots.Select(bot =>
            new NoviceBot(
                new BotProfile(
                    bot.Id,
                    bot.Name,
                    $"For your very first games: plays at random, but one turn in {Whole / bot.Share} grabs whatever "
                        + "scores the most right now, as Midas does. A plain algorithm: no AI.",
                    UsesAi: false
                ),
                bot.Share,
                random
            )
        ),
    ];

    private static ImmutableList<(string Id, string Name, int Share)> FaintBots { get; } =
    [
        ("morpheus", "Morpheus", 1),
        ("phaethon", "Phaethon", 2),
        ("tantalus", "Tantalus", 4),
    ];

    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this.Now().Seize(request, cancellationToken);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this.Now().Decide(request, cancellationToken);

    /// <summary>Who decides this time: Midas, as often as the bot's share says; else Random.</summary>
    private IBotPlaying Now() => this._random.Next(Whole) < this._share ? this._midas : this._aimless;
}
