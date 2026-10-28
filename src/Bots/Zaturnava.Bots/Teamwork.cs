using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Keeping out of an ally's way: what the bot's allies could win next on the board - the most any of
/// them could win by starting an exchange on a cell where an enemy piece stands, as <see cref="Careful"/>
/// counts one - so that a bot would rather not step into the line of an ally's attack, and would rather
/// step out of it.
/// </summary>
internal static class Teamwork
{
    /// <summary>
    /// Play on the board as <see cref="Greedy"/> does, but among the actions it would choose between,
    /// only those that leave the bot's allies the most to win.
    /// </summary>
    public static GameAction Choose(DecisionRequest request, Random random)
    {
        ImmutableList<(GameAction Action, int Threat)> threats =
            [.. Greedy.Choices(request).Select(action => (action, ThreatOf(request, After(request, action))))];
        int most = threats.Max(threat => threat.Threat);
        ImmutableList<GameAction> choices =
            [.. threats.Where(threat => threat.Threat == most).Select(threat => threat.Action)];
        return choices[random.Next(choices.Count)];
    }

    /// <summary>The most any of the bot's allies, not the bot itself, could win next in the state.</summary>
    public static int ThreatOf(DecisionRequest request, GameState state)
    {
        ILookup<CellId, Attack> allies =
            AttacksBy(request, state, seat => seat != request.Seat && state.Teams.AreAllied(seat, request.Seat));
        ILookup<CellId, Attack> enemies =
            AttacksBy(request, state, seat => !state.Teams.AreAllied(seat, request.Seat));
        return state.Position.Occupants
            .Where(occupant => !state.Teams.AreAllied(occupant.Value.Owner, request.Seat))
            .Select(occupant => Careful.Exchange(
                request.Rules.WorthOf(occupant.Value.Kind),
                Careful.WorthsOf(request, allies[occupant.Key]),
                Careful.WorthsOf(request, enemies[occupant.Key])
            ))
            .Append(0)
            .Max();
    }

    private static ILookup<CellId, Attack> AttacksBy(
        DecisionRequest request,
        GameState state,
        Func<SeatId, bool> isAttacking
    ) =>
        state.Seats
            .Where(seat => isAttacking(seat.Id))
            .SelectMany(seat => request.Rules.AttacksOf(state, seat.Id))
            .ToLookup(attack => attack.Target);

    private static GameState After(DecisionRequest request, GameAction action) =>
        request.Rules.Apply(request.State, action).State;
}
