using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Weighs a position as a whole (<see cref="Appraisal"/>), and looks ahead through the rest of its own turn,
/// the dice included (<see cref="Foresight"/>): so it rolls when what it expects of the dice beats the best
/// move it could make without them. Deals in prisoners as Solomon does. It weighs a position as it has
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
            + "dice than of the best move it could make without them. Trades prisoners as Solomon does. "
            + "A plain algorithm: no AI.",
        UsesAi: false
    );

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(
                Ending.AnswerTo(request)
                    ?? SolomonBot.AnswerTo(request)
                    ?? SolomonBot.FairTrade(request, false)
                    ?? Trading.BeforePassing(
                        request,
                        this.ForesightFor(request).Choose(request, this._random),
                        SolomonBot.FairTrade
                    )
            );

    private Foresight ForesightFor(DecisionRequest request) =>
        new(this._learned.GetValueOrDefault(request.Rules.Id, this._fallback), Depth, Beam);
}
