using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// What a position is worth to the bot's team, as Sage sees it: a weighted sum (<see cref="SageWeights"/>)
/// of what any ruleset lets it count - points, pieces, leaders, prisoners and thrones, each the team's less
/// its enemies' - less the worst an enemy could win next (as <see cref="Careful"/> counts it), plus the
/// most the team could, and how many more cells its pieces attack than its enemies' do; and of the
/// ruleset's own measures (<see cref="IPositionMeasuring"/>), the team's less its enemies', as far as
/// the weights weigh them. A game over is worth its result, by far more than any of that.
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
                + Reach(request, state, weights)
                + Measured(request, state, weights);

    /// <summary>
    /// What Sage counts in a position, unweighted: the points, pieces, leaders, prisoners and thrones, the
    /// worst an enemy could win next (as a loss), the most the team could, its mobility, and then each of
    /// the ruleset's own measures, in its order; each the team's less its enemies'. For a learner to weigh
    /// as it learns to.
    /// </summary>
    public static ImmutableList<double> FeaturesOf(DecisionRequest request, GameState state)
    {
        ILookup<CellId, Attack> ours = Careful.AttacksBy(request, state, isAllied: true);
        ILookup<CellId, Attack> theirs = Careful.AttacksBy(request, state, isAllied: false);
        ImmutableList<double> measures = request.Rules is IPositionMeasuring measuring
            ? [.. measuring.Measures.Select((_, at) => state.Seats
                .Sum(seat => Side(request, state, seat.Id) * measuring.MeasuresOf(state, seat.Id)[at])
            )]
            : [];
        return
        [
            Score(request, state), Material(request, state), Leaders(request, state), Prisoners(request, state),
            Thrones(request, state), -Careful.Exposure(request, state), Chances(request, state, ours, theirs),
            ours.Count - theirs.Count, .. measures,
        ];
    }

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

    /// <summary>
    /// The weighed chances (the most the team could win next, by starting an exchange where an enemy piece
    /// stands) and mobility (how many more cells its pieces attack than its enemies' do), from one count of
    /// what each side attacks.
    /// </summary>
    private static double Reach(DecisionRequest request, GameState state, SageWeights weights)
    {
        ILookup<CellId, Attack> ours = Careful.AttacksBy(request, state, isAllied: true);
        ILookup<CellId, Attack> theirs = Careful.AttacksBy(request, state, isAllied: false);
        return (weights.Chances * Chances(request, state, ours, theirs))
            + (weights.Mobility * (ours.Count - theirs.Count));
    }

    /// <summary>The ruleset's own measures, each the team's less its enemies', as the weights weigh them.</summary>
    private static double Measured(DecisionRequest request, GameState state, SageWeights weights) =>
        (request.Rules, weights.Measures.IsEmpty) switch
        {
            (IPositionMeasuring measuring, false) => state.Seats
                .Select(seat => (seat.Id, Values: measuring.MeasuresOf(state, seat.Id)))
                .Sum(measured => Side(request, state, measured.Id) * measuring.Measures
                    .Select((name, at) => weights.Measures.GetValueOrDefault(name) * measured.Values[at])
                    .Sum()
                ),
            _ => 0,
        };

    private static int Chances(
        DecisionRequest request,
        GameState state,
        ILookup<CellId, Attack> ours,
        ILookup<CellId, Attack> theirs
    ) =>
        state.Position.Occupants
            .Where(occupant => Side(request, state, occupant.Value.Owner) < 0)
            .Select(occupant => Careful.Exchange(
                request.Rules.WorthOf(occupant.Value.Kind),
                Careful.WorthsOf(request, ours[occupant.Key]),
                Careful.WorthsOf(request, theirs[occupant.Key])
            ))
            .Append(0)
            .Max();

    private static int Side(DecisionRequest request, GameState state, SeatId seat) =>
        state.Teams.AreAllied(seat, request.Seat) ? 1 : -1;
}
