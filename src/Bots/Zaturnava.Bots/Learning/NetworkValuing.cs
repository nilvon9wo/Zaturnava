using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Learning;

/// <summary>
/// A position judged by a <see cref="ValueNetwork"/>: how it expects the game to end for the team, from -1
/// to 1. Its guess at the margin is left aside: it swings by more between quiet moves than a taking gains. A game
/// over is judged by how it did end, a win or a loss counting <see cref="Decided"/> times as much as one expected,
/// and a little (<see cref="MarginWeight"/>) by how much the team leads: as Sage judges, no position, however good,
/// is worth more than winning, nor any worse than losing. A game going on
/// is judged worse by <see cref="Wariness"/> for each point an enemy could win by taking next (as
/// <see cref="Careful.Exposure"/> counts it): the network, judging where the bot's turn ends, cannot see the reply,
/// and would take a Pawn with a Raja only to lose the Raja. It is judged too, a little (<see cref="Steadiness"/>),
/// as Sage would, by <paramref name="weights"/> (<see cref="Appraisal"/>): a network sure of the end judges the moves
/// that lead there alike, or nearly, and would leave which to the slightest of its errors, a sure taking passed over
/// for a roll, a guarded piece moved off for nothing; Sage's count of what each side holds settles it. And it is
/// judged worse by <see cref="Blocking"/> for each of its team's pieces not a leader on a throne not the team's,
/// which only a leader can hold: one there only keeps the team's leaders off it.
/// </summary>
public sealed class NetworkValuing(PositionEncoder encoder, ValueNetwork network, SageWeights weights)
    : IPositionValuing
{
    /// <summary>How much a lead of a whole <see cref="ValueNetwork.MarginScale"/> counts, beside a win.</summary>
    public const double MarginWeight = 0.1;

    /// <summary>How much worse a position is judged for each point of a piece an enemy could win next.</summary>
    public const double Wariness = 0.05;

    /// <summary>How much more a game won (or lost) counts than one the network expects to be won (or lost).</summary>
    public const double Decided = 10;

    /// <summary>How much Sage's appraisal of a game going on counts, for each unit of it.</summary>
    public const double Steadiness = 0.002;

    /// <summary>How much worse a game going on is judged for each throne its team blocks.</summary>
    public const double Blocking = 0.02;

    private readonly PositionEncoder _encoder = encoder;
    private readonly ValueNetwork _network = network;
    private readonly SageWeights _weights = weights;

    public double ValueOf([Required] DecisionRequest request, [Required] GameState state) =>
        state.Outcome is { } ended
            ? (Decided * ResultOf(request.Seat, state, ended))
                + (MarginWeight * LeadOf(request.Seat, state) / ValueNetwork.MarginScale)
            : this.Judged(request, state)
                - (Wariness * Careful.Exposure(request, state))
                + (Steadiness * Appraisal.ValueOf(request, state, this._weights))
                - (Blocking * Blocked(request, state));

    /// <summary>The network's judgement of how a game going on will end.</summary>
    private double Judged(DecisionRequest request, GameState state) =>
        this._network.Evaluate(this._encoder.Encode(state, request.Seat)).Outcome;

    /// <summary>
    /// Whether the game ended now, points deciding, would be worth as much to the team as playing on, as the
    /// network judges playing on.
    /// </summary>
    public bool IsBetterEnded([Required] DecisionRequest request)
    {
        int lead = LeadOf(request.Seat, request.State);
        double ended = (Decided * Math.Sign(lead)) + (MarginWeight * lead / ValueNetwork.MarginScale);
        return ended >= this.ValueOf(request, request.State with { Outcome = null });
    }

    private static double ResultOf(SeatId seat, GameState state, GameOutcome outcome) =>
        (outcome.Winners.IsEmpty, outcome.Winners.Any(winner => state.Teams.AreAllied(winner, seat))) switch
        {
            (true, _) => 0,
            (_, true) => 1,
            _ => -1,
        };

    /// <summary>The points of the seat's team, less its enemies'.</summary>
    private static int LeadOf(SeatId seat, GameState state) =>
        state.Scores.Sum(score => (state.Teams.AreAllied(score.Key, seat) ? 1 : -1) * score.Value);

    /// <summary>How many thrones not its team's the team's pieces that are not leaders stand on.</summary>
    private static int Blocked(DecisionRequest request, GameState state) =>
        request.Rules.ThronesOf(state)
            .Where(throne => !state.Teams.AreAllied(throne.Key, request.Seat))
            .Select(throne => state.Position.PieceAt(throne.Value))
            .Count(piece => piece is not null
                && state.Teams.AreAllied(piece.Owner, request.Seat)
                && !request.Rules.IsLeader(piece.Kind)
            );
}
