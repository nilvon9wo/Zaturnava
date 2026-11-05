using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Which chance a bot takes, whoever decides (Proposed): of those that would leave its team's points further ahead
/// of its enemies' than they are, the one that leaves them furthest; none, if none would. So a leader face-off is
/// called against a leader worth more than the caller, never an even trade.
/// </summary>
internal static class Chances
{
    public static Task<GameAction?> Gainful(DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction?>(cancellationToken)
            : Task.FromResult(GainfulOf(request));

    private static GameAction? GainfulOf(DecisionRequest request)
    {
        int lead = LeadIn(request, request.State);
        return request.LegalActions
            .Select(chance => (Chance: chance, Lead: LeadIn(request, request.Rules.Apply(request.State, chance).State)))
            .Where(taken => taken.Lead > lead)
            .OrderByDescending(taken => taken.Lead)
            .Select(taken => taken.Chance)
            .FirstOrDefault();
    }

    /// <summary>The points of the bot's team, less its enemies', in <paramref name="state"/>.</summary>
    private static int LeadIn(DecisionRequest request, GameState state) =>
        state.Scores.Sum(score => (state.Teams.AreAllied(score.Key, request.Seat) ? 1 : -1) * score.Value);
}
