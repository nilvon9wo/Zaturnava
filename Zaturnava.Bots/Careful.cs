using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Play on the board that looks at the reply as well as the action: each legal action is worth what it
/// gains (as <see cref="Greedy"/> counts it), less the worst its opponents could do to the bot's team
/// once it is done. That is the most an enemy could win by starting an exchange on a cell where one of
/// the team's pieces stands, if both sides go on taking while it pays, or, if the bot's throne is
/// empty, what the enemy leader who could step onto it is worth. The dice and whose turn comes next
/// are left aside: any enemy might reply, with any piece. Half of what the action leaves its allies to
/// win next, or takes from them (<see cref="Teamwork"/>), counts too. Among actions worth the same, it
/// would rather do something than pass, and picks at random.
/// </summary>
internal static class Careful
{
    public static GameAction Choose(DecisionRequest request, Random random)
    {
        int allied = Teamwork.ThreatOf(request, request.State);
        ImmutableList<(GameAction Action, int Worth)> worths =
            [.. request.LegalActions.Select(action => (action, WorthOf(request, action, allied)))];
        int best = worths.Max(worth => worth.Worth);
        ImmutableList<GameAction> top = [.. worths.Where(worth => worth.Worth == best).Select(worth => worth.Action)];
        ImmutableList<GameAction> active = [.. top.Where(action => action is not PassAction)];
        ImmutableList<GameAction> choices = active.IsEmpty
            ? top
            : active;
        return choices[random.Next(choices.Count)];
    }

    /// <summary>
    /// Twice what the action gains less what it exposes, and what it leaves the bot's allies to win
    /// beyond the <paramref name="allied"/> they could before: half as much as the bot's own, being
    /// theirs to take, if no enemy gets in first.
    /// </summary>
    private static int WorthOf(DecisionRequest request, GameAction action, int allied)
    {
        GameState after = request.Rules.Apply(request.State, action).State;
        return (2 * (Greedy.Gain(request, action, after) - Exposure(request, after)))
            + Teamwork.ThreatOf(request, after)
            - allied;
    }

    /// <summary>The worst an enemy could do to the bot's team in reply: one exchange, or a throne taken.</summary>
    private static int Exposure(DecisionRequest request, GameState after)
    {
        ILookup<CellId, Attack> threats = AttacksBy(request, after, isAllied: false);
        ILookup<CellId, Attack> guards = AttacksBy(request, after, isAllied: true);
        IEnumerable<int> losses = after.Position.Occupants
            .Where(occupant => after.Teams.AreAllied(occupant.Value.Owner, request.Seat))
            .Select(occupant => Exchange(
                request.Rules.WorthOf(occupant.Value.Kind),
                WorthsOf(request, threats[occupant.Key]),
                WorthsOf(request, guards[occupant.Key])
            ));
        return losses.Append(0).Max() + ThroneThreat(request, after, threats);
    }

    /// <summary>
    /// What the side about to take wins, if it takes <paramref name="target"/> with the cheapest of its
    /// <paramref name="takers"/>, the other side retakes with its cheapest, and so on, each side stopping
    /// when going on would lose it more than it wins.
    /// </summary>
    internal static int Exchange(int target, ImmutableList<int> takers, ImmutableList<int> retakers) =>
        takers.IsEmpty
            ? 0
            : Math.Max(0, target - Exchange(takers[0], retakers, takers.RemoveAt(0)));

    /// <summary>
    /// What the enemy leader worth the most, among those that could step onto the bot's throne, is
    /// worth, if the throne is empty; nothing otherwise.
    /// </summary>
    private static int ThroneThreat(DecisionRequest request, GameState after, ILookup<CellId, Attack> threats) =>
        request.Rules.Thrones.TryGetValue(request.Seat, out CellId throne) && after.Position.PieceAt(throne) is null
            ? threats[throne]
                .Where(attack => request.Rules.IsLeader(attack.Attacker.Kind))
                .Select(attack => request.Rules.WorthOf(attack.Attacker.Kind))
                .Append(0)
                .Max()
            : 0;

    /// <summary>The attacks of the bot's team (<paramref name="isAllied"/>) or of its enemies, by target.</summary>
    private static ILookup<CellId, Attack> AttacksBy(DecisionRequest request, GameState after, bool isAllied) =>
        after.Seats
            .Where(seat => after.Teams.AreAllied(seat.Id, request.Seat) == isAllied)
            .SelectMany(seat => request.Rules.AttacksOf(after, seat.Id))
            .ToLookup(attack => attack.Target);

    /// <summary>What the attackers are worth, the cheapest first.</summary>
    internal static ImmutableList<int> WorthsOf(DecisionRequest request, IEnumerable<Attack> attacks) =>
        [.. attacks.Select(attack => request.Rules.WorthOf(attack.Attacker.Kind)).Order()];
}
