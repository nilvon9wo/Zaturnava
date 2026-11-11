using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Weighs a position as Sage does (<see cref="Appraisal"/>), but looks further ahead (<see cref="Tactics"/>): past
/// its own turn, through its enemies' and allies', each playing as it would - its enemies against it, its allies
/// for themselves - and the dice, however they fall. So it sees what an enemy would do in reply, where Sage only
/// counts what an enemy could take next. It looks as far as <paramref name="budget"/> lets it, following the
/// <paramref name="width"/> most promising actions at each decision. Ends games and deals in prisoners as Sage does.
/// It weighs a position as Sage has learned to for the ruleset, if it has; else as <paramref name="fallback"/> says.
/// </summary>
public sealed class TacticianBot(
    Random random,
    int budget,
    int width,
    SageWeights fallback,
    ImmutableDictionary<string, SageWeights> learned
) : IBotPlaying
{
    public const string BotId = "tactician";

    /// <summary>How many positions it may reach or judge for each decision (Proposed).</summary>
    public const int Budget = 4000;

    /// <summary>How many actions it follows further at each decision past its first (Proposed).</summary>
    public const int Width = 6;

    private readonly Random _random = random;
    private readonly int _budget = budget;
    private readonly int _width = width;
    private readonly SageWeights _fallback = fallback;
    private readonly ImmutableDictionary<string, SageWeights> _learned = learned;

    /// <summary>Tactician weighing positions as Sage has learned to, as far as it looks by default.</summary>
    public TacticianBot(Random random)
        : this(random, Budget, Width, SageWeights.Default, SageWeights.Learned)
    {
    }

    /// <summary>Tactician weighing every ruleset as <paramref name="weights"/> says.</summary>
    public TacticianBot(Random random, int budget, int width, SageWeights weights)
        : this(random, budget, width, weights, [])
    {
    }

    public BotProfile Profile { get; } = new(
        BotId,
        "Tactician",
        "Calculating: weighs the board as Sage does, but thinks past its own turn, through the turns of the "
            + "others and the dice, its enemies playing against it and its allies for themselves. So it sees the "
            + "reply coming, not just what could be taken next. A plain algorithm: no AI.",
        UsesAi: false
    );

    /// <summary>A chance that gains its team points (<see cref="Chances"/>).</summary>
    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Chances.Gainful(request, cancellationToken);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(this.Chosen(request));

    private GameAction Chosen(DecisionRequest request)
    {
        Appraiser valuing = new(this._learned.GetValueOrDefault(request.Rules.Id, this._fallback));
        Tactics tactics = new(valuing, this._budget, this._width);
        return SageBot.Play(
            request,
            Ending.AnswerTo(request),
            valuing,
            deciding => tactics.Choose(deciding, this._random)
        );
    }
}
