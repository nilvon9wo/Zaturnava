using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
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
    /// <paramref name="ours"/> points, and Yellow <paramref name="theirs"/>; Black's Elephant, sliding along the line,
    /// stands next to Yellow's Pawn, a point it may hope to take back.
    /// </summary>
    public static DecisionRequest Asked(int ours, int theirs)
    {
        AnswerEndAction agree = new(Black, true);
        AnswerEndAction refuse = new(Black, false);
        PieceKindId elephant = new("elephant");
        ScoringRules rules = new(ImmutableDictionary<GameAction, int>.Empty.Add(agree, 0).Add(refuse, 0))
        {
            Moves = ImmutableDictionary<PieceKindId, ImmutableList<MovementRule>>.Empty.Add(
                elephant,
                [MovementRule.Slide(DirectionSets.Orthogonal)]
            ),
        };
        GameState state = ScoringRules.State() with
        {
            Seats = [new Seat(Black, new Direction(1, 0))],
            Teams = TeamGraph.FromTeams([[Black, Green], [Yellow, new SeatId(3)]]),
            Scores = ImmutableDictionary<SeatId, int>.Empty.Add(Green, ours).Add(Yellow, theirs),
            Proposal = new EndProposal(Yellow, false, Black, null),
            Position = Position.Empty
                .Place(new CellId(0), new Piece(new PieceId(1), elephant, Black))
                .Place(new CellId(1), new Piece(new PieceId(2), new PieceKindId("pawn"), Yellow)),
        };
        return new DecisionRequest(rules, state, Black, rules.LegalActions(state));
    }
}
