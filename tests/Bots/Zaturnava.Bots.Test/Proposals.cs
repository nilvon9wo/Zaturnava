using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

/// <summary>Proposals to end the game, with seats 0 and 2 allied against 1 and 3.</summary>
internal static class Proposals
{
    public static readonly SeatId Black = ScoringRules.Seat;
    public static readonly SeatId Yellow = new(1);
    public static readonly SeatId Green = new(2);

    /// <summary>
    /// Seat 0 asked whether to end the game, which Yellow proposed, when Green, its ally, has
    /// <paramref name="ours"/> points, and Yellow <paramref name="theirs"/>.
    /// </summary>
    public static DecisionRequest Asked(int ours, int theirs)
    {
        AnswerEndAction agree = new(Black, true);
        AnswerEndAction refuse = new(Black, false);
        ScoringRules rules = new(ImmutableDictionary<GameAction, int>.Empty.Add(agree, 0).Add(refuse, 0));
        GameState state = ScoringRules.State() with
        {
            Teams = TeamGraph.FromTeams([[Black, Green], [Yellow, new SeatId(3)]]),
            Scores = ImmutableDictionary<SeatId, int>.Empty.Add(Green, ours).Add(Yellow, theirs),
            Proposal = new EndProposal(Yellow, false, Black, null),
        };
        return new DecisionRequest(rules, state, Black, rules.LegalActions(state));
    }
}
