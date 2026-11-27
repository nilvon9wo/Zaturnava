using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// A bot for beginners that never plays its worst, but seldom its best (the specification's D2: at least three bots
/// below 1200 Elo that are not random, a real progression of difficulty; Proposed, 2026-11-19, as the backlog said
/// to try): it judges each move as Sage does, but looks no further than the move itself, and plays one of the better
/// part of them at random, its share its strength. Epimetheus, the titan of afterthought, plays one of its best third;
/// Narcissus, who saw no further than himself, one of its better half. Offers, ransoms and proposals it answers as
/// Sage does. The bot-rating job rates each, so the picker shows how strong it turns out.
/// </summary>
public sealed class StumblerBot : IBotPlaying
{
    public const string EpimetheusId = "epimetheus";
    public const string NarcissusId = "narcissus";

    /// <summary>The share of its moves, the best first, Epimetheus chooses among, as a percentage.</summary>
    public const int EpimetheusShare = 33;

    /// <summary>The share of its moves, the best first, Narcissus chooses among, as a percentage.</summary>
    public const int NarcissusShare = 50;

    private const int Whole = 100;

    /// <summary>How far it looks: its own move, and no further.</summary>
    private const int Depth = 1;

    private readonly Random _random;
    private readonly int _share;
    private readonly IPositionValuing _valuing = new Appraiser(SageWeights.Default);

    private StumblerBot(BotProfile profile, int share, Random random)
    {
        this.Profile = profile;
        this._share = share;
        this._random = random;
    }

    public BotProfile Profile { get; }

    /// <summary>Plays one of its best third of moves, at random.</summary>
    public static StumblerBot Epimetheus([Required] Random random) =>
        new(
            new BotProfile(
                EpimetheusId,
                "Epimetheus",
                "For beginners. Weighs the board each move leaves (points, pieces, leaders, thrones, what could be "
                    + "taken next), then picks at random among its best third of moves: sensible, but rarely the "
                    + "best. As an ally: seldom throws a piece away. As an enemy: a fair test that forgives mistakes. "
                    + "A plain algorithm: no AI.",
                UsesAi: false
            ),
            EpimetheusShare,
            random
        );

    /// <summary>Plays one of its better half of moves, at random.</summary>
    public static StumblerBot Narcissus([Required] Random random) =>
        new(
            new BotProfile(
                NarcissusId,
                "Narcissus",
                "For beginners. Weighs the board each move leaves (points, pieces, leaders, thrones, what could be "
                    + "taken next), then picks at random among its better half of moves: often careless. As an ally: "
                    + "may leave a piece to be taken. As an enemy: gentle, but takes what you leave loose. A plain "
                    + "algorithm: no AI.",
                UsesAi: false
            ),
            NarcissusShare,
            random
        );

    /// <summary>A chance that gains its team points (<see cref="Chances"/>).</summary>
    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Chances.Gainful(request, cancellationToken);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(SageBot.Play(request, Ending.AnswerTo(request), this._valuing, this.Choose));

    /// <summary>One of the better part of its moves, as its share says (at least the best), at random.</summary>
    private GameAction Choose(DecisionRequest request)
    {
        ImmutableList<GameAction> ranked =
        [
            .. new Foresight(this._valuing, Depth, 1)
                .Worths(request)
                .OrderByDescending(worth => worth.Worth)
                .Select(worth => worth.Action),
        ];
        int kept = Math.Max(1, ((ranked.Count * this._share) + Whole - 1) / Whole);
        return ranked[this._random.Next(kept)];
    }
}
