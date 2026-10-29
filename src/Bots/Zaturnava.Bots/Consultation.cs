using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// A decision put into words for an advisor (<see cref="MoveQuestion"/>): the game, the bot's seat and team,
/// the scores, every piece on the board and every prisoner, and the dice still to use; then each choice, a
/// move by the cells it goes from and to, and what it takes.
/// </summary>
internal static class Consultation
{
    public static MoveQuestion Of(DecisionRequest request, ImmutableList<GameAction> choices) =>
        new(SituationOf(request), [.. choices.Select(choice => Described(request, choice))]);

    private static string SituationOf(DecisionRequest request)
    {
        GameState state = request.State;
        IRulesApplying rules = request.Rules;
        ICellNavigating board = rules.Topology;
        ImmutableList<SeatId> seats = [.. state.Seats.Select(seat => seat.Id)];
        IEnumerable<string> team = seats
            .Where(seat => state.Teams.AreAllied(seat, request.Seat))
            .Select(seat => SeatOf(rules, seat));
        IEnumerable<string> scores =
            seats.Select(seat => $"{SeatOf(rules, seat)} {state.Scores.GetValueOrDefault(seat)}");
        IEnumerable<string> pieces = state.Position.Occupants
            .OrderBy(occupant => occupant.Value.Owner.Value)
            .ThenBy(occupant => occupant.Key.Value)
            .Select(occupant => $"{PieceOf(rules, occupant.Value)} on {board.NameOf(occupant.Key)}");
        IEnumerable<string> prisoners = state.Prisoners
            .Select(prisoner => $"{SeatOf(rules, prisoner.Holder)} holds {PieceOf(rules, prisoner.Piece)}");
        return string.Join(
            '\n',
            $"You are playing {rules.Name}, as {SeatOf(rules, request.Seat)}.",
            $"Your team: {string.Join(", ", team)}.",
            $"Scores: {string.Join(", ", scores)}.",
            $"Pieces: {string.Join(", ", pieces)}.",
            $"Prisoners: {string.Join(", ", prisoners)}.",
            $"Dice rolled and not yet used: {string.Join(", ", state.Dice)}."
        );
    }

    private static string Described(DecisionRequest request, GameAction choice) =>
        choice is MoveAction move
            ? Moved(request, move)
            : Plain(choice.Describe());

    private static string Moved(DecisionRequest request, MoveAction move)
    {
        IRulesApplying rules = request.Rules;
        ICellNavigating board = rules.Topology;
        string moved = $"move from {board.NameOf(move.From)} to {board.NameOf(move.To)}";
        return request.State.Position.PieceAt(move.To) is { } taken
            ? $"{moved}, taking {PieceOf(rules, taken)}"
            : moved;
    }

    /// <summary>Any other action, by its kind and arguments: "roll", "pass", "promote kind=horse".</summary>
    private static string Plain(ActionDescriptor action) =>
        string.Join(
            ' ',
            [
                action.Kind,
                .. action.Arguments
                    .Where(argument => argument.Key != ActionDescriptor.ActorArgument)
                    .Select(argument => $"{argument.Key}={argument.Value}"),
            ]
        );

    private static string PieceOf(IRulesApplying rules, Piece piece) =>
        $"{SeatOf(rules, piece.Owner)}'s {rules.NameOf(piece.Kind)}";

    private static string SeatOf(IRulesApplying rules, SeatId seat) => rules.NameOf(seat);
}
