using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Weighs a position as a whole (<see cref="Appraisal"/>), and looks ahead through the rest of its own turn,
/// the dice included (<see cref="Foresight"/>): so it rolls when what it expects of the dice beats the best
/// move it could make without them. Deals in prisoners as Solomon does. Its weights are a starting guess,
/// for playing itself to tune.
/// </summary>
public sealed class SageBot(Random random, SageWeights weights) : IBotPlaying
{
    public const string BotId = "sage";

    private const int Depth = 3;
    private const int Beam = 3;

    private readonly Random _random = random;
    private readonly Foresight _foresight = new(weights, Depth, Beam);

    public SageBot(Random random)
        : this(random, SageWeights.Default)
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
                        this._foresight.Choose(request, this._random),
                        SolomonBot.FairTrade
                    )
            );
}
