using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Commanding every piece on the board, on one's own turn, scores a bonus (rules.md §5): once, until every
/// seat has a piece on the board again. A retired seat's Raja counts for neither (§10). It is reckoned as the
/// seat deciding on its own turn sees what it commands, after each action.
/// </summary>
internal sealed class EveryPiece(ThroneControl control)
{
    private readonly ThroneControl _control = control;

    public Transition Scored(Transition after)
    {
        GameState state = after.State;
        ImmutableList<Piece> pieces =
        [
            .. state.Position.Occupants.Values.Where(piece => !state.Retired.Contains(piece.Owner)),
        ];
        bool isEverySeatOnBoard = state.Seats.All(seat => pieces.Any(piece => piece.Owner == seat.Id));
        SeatId? commander = OwnTurnOf(state) is { } turn && this.CommandsAll(state, turn, pieces)
            ? turn
            : null;
        return (state.Outcome, isEverySeatOnBoard, state.IsEveryPieceScored, commander) switch
        {
            ({ }, _, _, _) => after,
            (null, true, _, _) => after with { State = state with { IsEveryPieceScored = false } },
            (null, false, false, { } scorer) => Chaturaji2027Rules.Scored(
                after with { State = state with { IsEveryPieceScored = true } },
                scorer,
                Chaturaji2027Rules.EveryPieceBonus,
                new EveryPieceCommanded(scorer, Chaturaji2027Rules.EveryPieceBonus)
            ),
            _ => after,
        };
    }

    /// <summary>The seat deciding on its own turn, not for another, nor answering one; null if none is.</summary>
    private static SeatId? OwnTurnOf(GameState state) =>
        (state.ActingFor, state.Offer, state.Proposal, state.Returning) switch
        {
            (null, null, null, null) => state.DecidingSeat,
            _ => null,
        };

    private bool CommandsAll(GameState state, SeatId seat, ImmutableList<Piece> pieces)
    {
        ImmutableHashSet<SeatId> armies =
        [
            seat, .. this._control.CommandedBy(state, seat).Select(command => command.Army),
        ];
        return pieces.All(piece => armies.Contains(piece.Owner));
    }
}
