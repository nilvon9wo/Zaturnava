using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Weighs a position as a whole (<see cref="Appraisal"/>), and looks ahead through the rest of its own turn,
/// the dice included (<see cref="Foresight"/>): so it rolls when what it expects of the dice beats the best
/// move it could make without them. Deals in prisoners by the same measure. It weighs a position as it has
/// learned to for the ruleset, by playing itself, if it has; else as <paramref name="fallback"/> says.
/// </summary>
public sealed class SageBot(
    Random random,
    SageWeights fallback,
    ImmutableDictionary<string, SageWeights> learned
) : IBotPlaying
{
    public const string BotId = "sage";

    private const int Depth = 3;
    private const int Beam = 3;

    private readonly Random _random = random;
    private readonly SageWeights _fallback = fallback;
    private readonly ImmutableDictionary<string, SageWeights> _learned = learned;

    /// <summary>Sage as it has learned to play each ruleset.</summary>
    public SageBot(Random random)
        : this(random, SageWeights.Default, SageWeights.Learned)
    {
    }

    /// <summary>Sage weighing every ruleset as <paramref name="weights"/> says, as one trying them out.</summary>
    public SageBot(Random random, SageWeights weights)
        : this(random, weights, [])
    {
    }

    public BotProfile Profile { get; } = new(
        BotId,
        "Sage",
        "Thoughtful: weighs the whole board (points, pieces, Rajas, prisoners, thrones, and what each side "
            + "could take next) and thinks through the rest of its turn, rolling only when it expects more of the "
            + "dice than of the best move it could make without them. Trades prisoners when the board would be "
            + "better for it. A plain algorithm: no AI.",
        UsesAi: false
    );

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(
                Play(
                    request,
                    Ending.AnswerTo(request),
                    new Appraiser(this._learned.GetValueOrDefault(request.Rules.Id, this._fallback)),
                    this._random
                )
            );

    /// <summary>
    /// Sage's play, with positions judged by <paramref name="valuing"/>: <paramref name="ending"/>, if it has
    /// answered a proposal to end the game; else its answer to an offer, or an offer of its own, if it would
    /// make one; else what it would most expect of the rest of its turn, as <see cref="Foresight"/> sees it,
    /// trading before it passes. It weighs a ransom by the whole position once it is done
    /// (<see cref="ByPosition"/>): it accepts an offer that leaves it no worse off, and offers its allies' pieces
    /// back, then the trade that would leave it best off, if any leaves it better.
    /// </summary>
    internal static GameAction Play(
        DecisionRequest request,
        GameAction? ending,
        IPositionValuing valuing,
        Random random
    )
    {
        ByPosition judge = new(valuing);
        return ending
            ?? Trading.AnswerTo(request, 0, judge)
            ?? BestTrade(request, false, judge)
            ?? Trading.BeforePassing(
                request,
                new Foresight(valuing, Depth, Beam).Choose(request, random),
                (passing, isPassing) => BestTrade(passing, isPassing, judge)
            );
    }

    /// <summary>
    /// A gift to an ally, if one may be made; else, of the trades that gain anything, the most even, then the one
    /// that gains the most.
    /// </summary>
    private static OfferAction? BestTrade(DecisionRequest request, bool isPassing, ByPosition judge) =>
        Trading.TradesOf(request, isPassing, judge)
            .Where(trade => trade.IsGift || trade.Gain > 0)
            .OrderByDescending(trade => trade.IsGift)
            .ThenBy(trade => trade.Imbalance)
            .ThenByDescending(trade => trade.Gain)
            .FirstOrDefault()?.Offer;
}
