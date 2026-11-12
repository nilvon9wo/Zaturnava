using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// What a position is worth to the bot's team, as Sage sees it: a weighted sum (<see cref="SageWeights"/>)
/// of what any ruleset lets it count - points, pieces, leaders, prisoners and thrones, each the bot's own, its
/// allies' and its enemies', weighed apart (<see cref="Sided"/>) - less the worst an enemy could win next (as
/// <see cref="Careful"/> counts it), plus what the team could still win once its enemies have moved (each saving
/// what it can: <see cref="Threats"/>), and how many cells its pieces attack, and its
/// enemies'; and of the ruleset's own measures (<see cref="IPositionMeasuring"/>), each side's, as far as the
/// weights weigh them. A game over is worth its result, by far more than any of that.
/// </summary>
internal static class Appraisal
{
    private const double Decided = 1000;

    public static double ValueOf(DecisionRequest request, GameState state, SageWeights weights) =>
        state.Outcome is { } outcome
            ? ResultOf(request, outcome) + Score(request, state).Net
            : Judged(
                request,
                state,
                weights,
                Careful.AttacksBy(request, state, isAllied: true),
                Careful.AttacksBy(request, state, isAllied: false)
            );

    /// <summary>
    /// What Sage counts in a position, unweighted: the points, pieces, leaders, prisoners and thrones, the
    /// worst an enemy could win next (as a loss), the most the team could, its mobility, and then each of
    /// the ruleset's own measures, in its order; each the team's less its enemies' (<see cref="Sided.Net"/>). For a
    /// learner to weigh as it learns to.
    /// </summary>
    public static ImmutableList<double> FeaturesOf(DecisionRequest request, GameState state)
    {
        ILookup<CellId, Attack> ours = Careful.AttacksBy(request, state, isAllied: true);
        ILookup<CellId, Attack> theirs = Careful.AttacksBy(request, state, isAllied: false);
        ImmutableList<double> measures = request.Rules is IPositionMeasuring measuring
            ? [.. Measures(request, state, measuring).Select(measure => measure.Net)]
            : [];
        return
        [
            Score(request, state).Net, Material(request, state).Net, Leaders(request, state).Net,
            Prisoners(request, state).Net, Thrones(request, state).Net, -Careful.Exposure(request, state, theirs, ours),
            Chances(request, state, ours, theirs), Mobility(request, ours, theirs).Net, .. measures,
        ];
    }

    private static double ResultOf(DecisionRequest request, GameOutcome outcome) =>
        (outcome.Winners.IsEmpty, outcome.Winners.Contains(request.Seat)) switch
        {
            (true, _) => 0,
            (_, true) => Decided,
            _ => -Decided,
        };

    /// <summary>Each side's points.</summary>
    private static Sided Score(DecisionRequest request, GameState state) =>
        Tally(request, state, state.Scores.Select(score => (score.Key, (double)score.Value)));

    /// <summary>What each side's pieces on the board are worth.</summary>
    private static Sided Material(DecisionRequest request, GameState state) =>
        Tally(
            request,
            state,
            state.Position.Occupants.Values.Select(piece => (piece.Owner, (double)request.Rules.WorthOf(piece.Kind)))
        );

    /// <summary>How many leaders each side has on the board.</summary>
    private static Sided Leaders(DecisionRequest request, GameState state) =>
        Tally(
            request,
            state,
            state.Position.Occupants.Values
                .Where(piece => request.Rules.IsLeader(piece.Kind))
                .Select(piece => (piece.Owner, 1d))
        );

    /// <summary>What the pieces each side holds of others not its allies are worth.</summary>
    private static Sided Prisoners(DecisionRequest request, GameState state) =>
        Tally(
            request,
            state,
            state.Prisoners
                .Where(prisoner => !state.Teams.AreAllied(prisoner.Piece.Owner, prisoner.Holder))
                .Select(prisoner => (prisoner.Holder, (double)request.Rules.WorthOf(prisoner.Piece.Kind)))
        );

    /// <summary>How many thrones not their allies' each side's leaders sit on.</summary>
    private static Sided Thrones(DecisionRequest request, GameState state) =>
        Tally(
            request,
            state,
            request.Rules.ThronesOf(state)
                .Select(throne => (Throne: throne.Key, Occupant: state.Position.PieceAt(throne.Value)))
                .Where(held => held.Occupant is not null
                    && request.Rules.IsLeader(held.Occupant.Kind)
                    && !state.Teams.AreAllied(held.Occupant.Owner, held.Throne)
                )
                .Select(held => (held.Occupant!.Owner, 1d))
        );

    /// <summary>
    /// How many cells each side's pieces attack: the bot's own; those its allies' attack besides; and its enemies'.
    /// </summary>
    private static Sided Mobility(DecisionRequest request, ILookup<CellId, Attack> ours, ILookup<CellId, Attack> theirs)
    {
        int own = ours.Count(cell => cell.Any(attack => attack.Attacker.Owner == request.Seat));
        return new Sided(own, ours.Count - own, theirs.Count);
    }

    /// <summary>
    /// A game not over, as the weights weigh it, from one count of what each side attacks: the team's
    /// (<paramref name="ours"/>) and its enemies' (<paramref name="theirs"/>), which the exposure, the chances (the
    /// most the team could win next, by starting an exchange where an enemy piece stands) and mobility
    /// (<see cref="Mobility"/>) all count from.
    /// </summary>
    private static double Judged(
        DecisionRequest request,
        GameState state,
        SageWeights weights,
        ILookup<CellId, Attack> ours,
        ILookup<CellId, Attack> theirs
    ) =>
        weights.Score.Weigh(Score(request, state))
            + weights.Material.Weigh(Material(request, state))
            + weights.Leaders.Weigh(Leaders(request, state))
            + weights.Prisoners.Weigh(Prisoners(request, state))
            + weights.Thrones.Weigh(Thrones(request, state))
            - (weights.Exposure * Careful.Exposure(request, state, theirs, ours))
            + (weights.Chances * Threats(request, state, ours, theirs))
            + weights.Mobility.Weigh(Mobility(request, ours, theirs))
            + Measured(request, state, weights);

    /// <summary>
    /// The ruleset's own measures, each side's, as the weights weigh them; not measured at all when the weights
    /// weigh none of them, as some take long to count (a ruleset's pieces' moves, say).
    /// </summary>
    private static double Measured(DecisionRequest request, GameState state, SageWeights weights) =>
        (request.Rules, weights.Measures.Values.Any(weight => weight != default)) switch
        {
            (IPositionMeasuring measuring, true) => measuring.Measures
                .Zip(Measures(request, state, measuring))
                .Sum(measured => weights.Measures.GetValueOrDefault(measured.First).Weigh(measured.Second)),
            _ => 0,
        };

    /// <summary>Each side's measures, in the order the ruleset offers them: each seat's counted once.</summary>
    private static IEnumerable<Sided> Measures(DecisionRequest request, GameState state, IPositionMeasuring measuring)
    {
        ImmutableList<(SeatId Seat, ImmutableList<double> Measures)> measured =
            [.. state.Seats.Select(seat => (seat.Id, measuring.MeasuresOf(state, seat.Id)))];
        return measuring.Measures.Select((_, at) =>
            Tally(request, state, measured.Select(each => (each.Seat, each.Measures[at])))
        );
    }

    /// <summary>
    /// The most the team could win next by starting an exchange where an enemy piece stands, as if no enemy moved
    /// first: what a learner was taught to weigh (<see cref="FeaturesOf"/>), and so kept as it was.
    /// </summary>
    private static int Chances(
        DecisionRequest request,
        GameState state,
        ILookup<CellId, Attack> ours,
        ILookup<CellId, Attack> theirs
    ) =>
        state.Position.Occupants
            .Where(occupant => !state.Teams.AreAllied(occupant.Value.Owner, request.Seat))
            .Select(occupant => Careful.Exchange(
                request.Rules.WorthOf(occupant.Value.Kind),
                Careful.WorthsOf(request, ours[occupant.Key]),
                Careful.WorthsOf(request, theirs[occupant.Key])
            ))
            .Append(0)
            .Max();

    /// <summary>
    /// What the team may still win, once its enemies have moved, by starting an exchange where an enemy piece stands
    /// (Proposed; the user's ruling of 2026-11-12, that threatened pieces move before they can be taken). Every enemy
    /// moves before the team's next turn, so: a piece threatened that could itself take one of the team's will take
    /// it and be gone, and wins the team nothing; and each enemy saves the piece it would lose most, so only its
    /// second-worst threat counts (a fork). The most of that over the enemies.
    /// </summary>
    private static int Threats(
        DecisionRequest request,
        GameState state,
        ILookup<CellId, Attack> ours,
        ILookup<CellId, Attack> theirs
    )
    {
        ImmutableHashSet<PieceId> takers =
        [
            .. state.Position.Occupants
                .Where(occupant => state.Teams.AreAllied(occupant.Value.Owner, request.Seat))
                .SelectMany(occupant => theirs[occupant.Key])
                .Select(attack => attack.Attacker.Id),
        ];
        return state.Position.Occupants
            .Where(occupant => !state.Teams.AreAllied(occupant.Value.Owner, request.Seat)
                && !takers.Contains(occupant.Value.Id)
            )
            .Select(occupant => (occupant.Value.Owner, Won: Careful.Exchange(
                request.Rules.WorthOf(occupant.Value.Kind),
                Careful.WorthsOf(request, ours[occupant.Key]),
                Careful.WorthsOf(request, theirs[occupant.Key])
            )))
            .GroupBy(threat => threat.Owner, threat => threat.Won)
            .Select(threats => threats.OrderDescending().Skip(1).Append(0).Max())
            .Append(0)
            .Max();
    }

    /// <summary>How much of something each side has: the bot's own seat, its allies, its enemies.</summary>
    private static Sided Tally(
        DecisionRequest request,
        GameState state,
        IEnumerable<(SeatId Seat, double Value)> counted
    )
    {
        ILookup<int, double> sides = counted.ToLookup(
            each => each.Seat == request.Seat ? 0 : state.Teams.AreAllied(each.Seat, request.Seat) ? 1 : 2,
            each => each.Value
        );
        return new Sided(sides[0].Sum(), sides[1].Sum(), sides[2].Sum());
    }
}
