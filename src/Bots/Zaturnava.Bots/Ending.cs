using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>How a bot answers a proposal to end the game: it agrees if its team leads on points.</summary>
internal static class Ending
{
    /// <summary>The answer to the proposal waiting, if one is; null if none is.</summary>
    public static GameAction? AnswerTo(DecisionRequest request) =>
        request.State.Proposal is null
            ? null
            : new AnswerEndAction(request.Seat, IsAhead(request));

    private static bool IsAhead(DecisionRequest request)
    {
        GameState state = request.State;
        ILookup<bool, int> scores =
            state.Scores.ToLookup(score => state.Teams.AreAllied(score.Key, request.Seat), score => score.Value);
        return scores[true].Sum() > scores[false].Sum();
    }
}
