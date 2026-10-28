using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// What a position is worth to the bot's team, as Sage sees it: a weighted sum (<see cref="SageWeights"/>)
/// of what any ruleset lets it count - points, pieces, leaders, prisoners and thrones, each the team's less
/// its enemies' - less the worst an enemy could win next (as <see cref="Careful"/> counts it), and plus the
/// most the team could. A game over is worth its result, by far more than any of that.
/// </summary>
internal static class Appraisal
{
    private const double Decided = 1000;

    public static double ValueOf(DecisionRequest request, GameState state, SageWeights weights) =>
        state.Outcome is { } outcome
            ? ResultOf(request, outcome) + Score(request, state)
            : (weights.Score * Score(request, state))
                + (weights.Material * Material(request, state))
                + (weights.Leaders * Leaders(request, state))
                + (weights.Prisoners * Prisoners(request, state))
                + (weights.Thrones * Thrones(request, state))
                - (weights.Exposure * Careful.Exposure(request, state))
                + (weights.Chances * Chances(request, state));

    private static double ResultOf(DecisionRequest request, GameOutcome outcome) =>
        (outcome.Winners.IsEmpty, outcome.Winners.Contains(request.Seat)) switch
        {
            (true, _) => 0,
            (_, true) => Decided,
            _ => -Decided,
        };

    /// <summary>The points of the bot's team, less its enemies'.</summary>
    private static int Score(DecisionRequest request, GameState state) =>
        state.Scores.Sum(score => Side(request, state, score.Key) * score.Value);

    /// <summary>What the team's pieces on the board are worth, less its enemies'.</summary>
    private static int Material(DecisionRequest request, GameState state) =>
        state.Position.Occupants.Values
            .Sum(piece => Side(request, state, piece.Owner) * request.Rules.WorthOf(piece.Kind));

    /// <summary>How many leaders the team has on the board, less its enemies.</summary>
    private static int Leaders(DecisionRequest request, GameState state) =>
        state.Position.Occupants.Values
            .Where(piece => request.Rules.IsLeader(piece.Kind))
            .Sum(piece => Side(request, state, piece.Owner));

    /// <summary>What the enemy pieces the team holds are worth, less what its own held by enemies are.</summary>
    private static int Prisoners(DecisionRequest request, GameState state) =>
        state.Prisoners
            .Where(prisoner => !state.Teams.AreAllied(prisoner.Piece.Owner, prisoner.Holder))
            .Sum(prisoner => Side(request, state, prisoner.Holder) * request.Rules.WorthOf(prisoner.Piece.Kind));

    /// <summary>How many enemy thrones the team's leaders sit on, less how many of its own enemy leaders do.</summary>
    private static int Thrones(DecisionRequest request, GameState state) =>
        request.Rules.Thrones
            .Select(throne => (Throne: throne.Key, Occupant: state.Position.PieceAt(throne.Value)))
            .Where(held => held.Occupant is not null
                && request.Rules.IsLeader(held.Occupant.Kind)
                && !state.Teams.AreAllied(held.Occupant.Owner, held.Throne)
            )
            .Sum(held => Side(request, state, held.Occupant!.Owner));

    /// <summary>The most the team could win next, by starting an exchange where an enemy piece stands.</summary>
    private static int Chances(DecisionRequest request, GameState state)
    {
        ILookup<CellId, Attack> ours = Careful.AttacksBy(request, state, isAllied: true);
        ILookup<CellId, Attack> theirs = Careful.AttacksBy(request, state, isAllied: false);
        return state.Position.Occupants
            .Where(occupant => Side(request, state, occupant.Value.Owner) < 0)
            .Select(occupant => Careful.Exchange(
                request.Rules.WorthOf(occupant.Value.Kind),
                Careful.WorthsOf(request, ours[occupant.Key]),
                Careful.WorthsOf(request, theirs[occupant.Key])
            ))
            .Append(0)
            .Max();
    }

    private static int Side(DecisionRequest request, GameState state, SeatId seat) =>
        state.Teams.AreAllied(seat, request.Seat) ? 1 : -1;
}
