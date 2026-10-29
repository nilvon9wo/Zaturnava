using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// What a bot may choose: anything legal but handing its turn to an ally, as it plays its own, retiring, as it
/// plays to the end, or taking a piece of its own team, as an enemy army it commands may.
/// </summary>
internal static class Playable
{
    public static ImmutableList<GameAction> Of(DecisionRequest request) =>
        [
            .. request.LegalActions.Where(action =>
                action is not (HandOverAction or RetireAction) && !TakesOwnTeam(request, action)
            ),
        ];

    private static bool TakesOwnTeam(DecisionRequest request, GameAction action) =>
        action is MoveAction move
        && request.State.Position.PieceAt(move.To) is { } taken
        && request.State.Teams.AreAllied(taken.Owner, request.Seat);
}
