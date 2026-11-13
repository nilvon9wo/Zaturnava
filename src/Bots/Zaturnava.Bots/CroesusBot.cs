using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Midas, a little less careless (Stated 2026-11-13: "A less careless Midas might be an additional bot"): rich and
/// greedy, like the king who learned caution only from his ruin, it takes whichever legal action scores the most at
/// once, as Midas does; but when nothing scores, it would rather make a move that leaves none of its team's pieces
/// newly open to capture (Proposed: no more exposed, as <see cref="Careful"/> weighs the worst an enemy could do in
/// reply, than before), moving at random among those, and only if there are none at random among all. It still never
/// weighs what it gives away for what it grabs. It trades prisoners, answers proposals and seizes chances as Midas.
/// </summary>
public sealed class CroesusBot(Random random) : IBotPlaying
{
    public const string BotId = "croesus";

    private readonly Random _random = random;

    public BotProfile Profile { get; } = new(
        BotId,
        "Croesus",
        "Greedy like Midas, but a little less careless: grabs whatever scores the most right now; otherwise, "
            + "moves so as to leave no piece newly open to capture, when it can. A plain algorithm: no AI.",
        UsesAi: false
    );

    /// <summary>A chance that gains its team points (<see cref="Chances"/>).</summary>
    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Chances.Gainful(request, cancellationToken);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(
                Ending.AnswerTo(request)
                    ?? Trading.AnswerTo(request, 0, ByWorth.AndPoints)
                    ?? Trading.TradesOf(request, ByWorth.AndPoints)
                        .Where(trade => trade.Gain > 0)
                        .MaxBy(trade => trade.Gain)?.Offer
                    ?? this.Played(request)
            );

    /// <summary>
    /// Midas's choice, if anything scores; else, of Midas's choices, those that leave the team no more exposed than
    /// now, if any do.
    /// </summary>
    private GameAction Played(DecisionRequest request)
    {
        (ImmutableList<GameAction> choices, bool scores) = Greedy.Weighed(request);
        ImmutableList<GameAction> safe = scores ? choices : Safe(request, choices);
        ImmutableList<GameAction> kept = safe.IsEmpty ? choices : safe;
        return kept[this._random.Next(kept.Count)];
    }

    private static ImmutableList<GameAction> Safe(DecisionRequest request, ImmutableList<GameAction> choices)
    {
        int now = Careful.Exposure(request, request.State);
        return
        [
            .. choices.Where(action =>
                Careful.Exposure(request, request.Rules.Apply(request.State, action).State) <= now
            ),
        ];
    }
}
