using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// What a bot may choose: anything legal but handing its turn to an ally, as it plays its own, or taking a piece of
/// its own team, as an enemy army it commands may. It retires only to save a draw (<see cref="SavesDraw"/>), and then
/// does nothing else.
/// </summary>
internal static class Playable
{
    public static ImmutableList<GameAction> Of(DecisionRequest request) =>
        request.LegalActions.OfType<RetireAction>().FirstOrDefault() is { } retiring && SavesDraw(request, retiring)
            ? [retiring]
            : [
                .. request.LegalActions.Where(action =>
                    action is not (HandOverAction or RetireAction) && !TakesOwnTeam(request, action)
                ),
            ];

    /// <summary>
    /// Whether retiring ends the game at once in a draw (its team left only retired Rajas, and not behind on points:
    /// rules.md §10) while the enemies have more pieces on the board than the team: a lone Raja that plays on can then
    /// only lose the draw, its Raja taken or the points slipping (the user's report of 2026-11-27: Tactician, its ally
    /// retired with the scores level, played on alone for six turns until the enemy pulled ahead, and lost).
    /// </summary>
    private static bool SavesDraw(DecisionRequest request, RetireAction retiring)
    {
        GameState state = request.State;
        int ours = state.Position.Occupants.Values.Count(piece =>
            state.Teams.AreAllied(piece.Owner, request.Seat) && !state.Retired.Contains(piece.Owner)
        );
        int theirs = state.Position.Occupants.Values.Count(piece =>
            !state.Teams.AreAllied(piece.Owner, request.Seat) && !state.Retired.Contains(piece.Owner)
        );
        return theirs > ours && request.Rules.ApplyLegal(state, retiring).State.Outcome is { IsDraw: true };
    }

    /// <summary>
    /// Whether the action takes a piece of the bot's own team: moves onto it, and it is gone from the board after. Not
    /// a swap back to back (rules.md §3), which moves onto an ally's piece but leaves it on the board, behind (the
    /// bots never swapped, every such move counted a taking, till 2026-11-25).
    /// </summary>
    private static bool TakesOwnTeam(DecisionRequest request, GameAction action) =>
        action is MoveAction move
        && request.State.Position.PieceAt(move.To) is { } taken
        && request.State.Teams.AreAllied(taken.Owner, request.Seat)
        && request.Rules.ApplyLegal(request.State, action).State.Position.Occupants.Values
            .All(piece => piece.Id != taken.Id);
}
