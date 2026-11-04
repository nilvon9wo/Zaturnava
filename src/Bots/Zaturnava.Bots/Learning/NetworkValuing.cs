using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Learning;

/// <summary>
/// A position judged by a <see cref="ValueNetwork"/>: how it expects the game to end for the team, from -1
/// to 1, and a little (<see cref="MarginWeight"/>) by how much it expects the team to lead. A game over is
/// judged by how it did end, a win or a loss counting <see cref="Decided"/> times as much as one expected: as
/// Sage judges, no position, however good, is worth more than winning, nor any worse than losing. A game going on
/// is judged worse by <see cref="Wariness"/> for each point an enemy could win by taking next (as
/// <see cref="Careful.Exposure"/> counts it): the network, judging where the bot's turn ends, cannot see the reply,
/// and would take a Pawn with a Raja only to lose the Raja.
/// </summary>
public sealed class NetworkValuing(PositionEncoder encoder, ValueNetwork network) : IPositionValuing
{
    /// <summary>How much a margin of a whole <see cref="ValueNetwork.MarginScale"/> counts, beside a win.</summary>
    public const double MarginWeight = 0.1;

    /// <summary>How much worse a position is judged for each point of a piece an enemy could win next.</summary>
    public const double Wariness = 0.05;

    /// <summary>How much more a game won (or lost) counts than one the network expects to be won (or lost).</summary>
    public const double Decided = 10;

    private readonly PositionEncoder _encoder = encoder;
    private readonly ValueNetwork _network = network;

    public double ValueOf([Required] DecisionRequest request, [Required] GameState state) =>
        state.Outcome is { } ended
            ? (Decided * ResultOf(request.Seat, state, ended))
                + (MarginWeight * LeadOf(request.Seat, state) / ValueNetwork.MarginScale)
            : this.Judged(request, state) - (Wariness * Careful.Exposure(request, state));

    /// <summary>The network's judgement of a game going on.</summary>
    private double Judged(DecisionRequest request, GameState state)
    {
        (double outcome, double margin) = this._network.Evaluate(this._encoder.Encode(state, request.Seat));
        return outcome + (MarginWeight * margin);
    }

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
}
