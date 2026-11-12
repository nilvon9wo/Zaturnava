using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Play on the board that looks at the reply as well as the action: each legal action is worth what it
/// gains (as <see cref="Greedy"/> counts it), less the worst its opponents could do to the bot's team
/// once it is done. That is the most an enemy could win by starting an exchange on a cell where one of
/// the team's pieces stands, if both sides go on taking while it pays, or, if the bot's throne is
/// empty, what the enemy leader who could step onto it is worth. A piece on the bot's throne does not
/// keep an enemy leader off it, but only makes it take that piece to get there: such a taking wins the
/// piece and the throne both. The dice are left aside, but not whose turn comes next: a seat of the team
/// that moves before an enemy saves the piece it would lose most to it (<see cref="LossOfSeat"/>). Half
/// of what the action leaves its allies to win next, or takes from them (<see cref="Teamwork"/>), counts
/// too. Among actions worth the same, it would rather do something than pass, and picks at random.
/// </summary>
internal static class Careful
{
    public static GameAction Choose(DecisionRequest request, Random random)
    {
        int allied = Teamwork.ThreatOf(request, request.State);
        ImmutableList<(GameAction Action, int Worth)> worths =
            [.. Playable.Of(request).Select(action => (action, WorthOf(request, action, allied)))];
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
    internal static int Exposure(DecisionRequest request, GameState after) =>
        Exposure(
            request,
            after,
            AttacksBy(request, after, isAllied: false),
            AttacksBy(request, after, isAllied: true)
        );

    /// <summary>
    /// The worst an enemy could do to the bot's team in reply, the enemies' attacks (<paramref name="threats"/>) and
    /// the team's (<paramref name="guards"/>) counted already.
    /// </summary>
    internal static int Exposure(
        DecisionRequest request,
        GameState after,
        ILookup<CellId, Attack> threats,
        ILookup<CellId, Attack> guards
    )
    {
        ImmutableDictionary<SeatId, int> turns = TurnsOf(request, after);
        IEnumerable<int> losses = after.Position.Occupants
            .Where(occupant => after.Teams.AreAllied(occupant.Value.Owner, request.Seat))
            .GroupBy(occupant => occupant.Value.Owner)
            .Select(pieces => LossOfSeat(request, after, turns, pieces, threats, guards));
        return losses.Append(0).Max() + ThroneThreat(request, after, threats);
    }

    /// <summary>
    /// The worst an enemy could do to the pieces of one seat of the bot's team (<paramref name="pieces"/>), its turn
    /// coming as <paramref name="turns"/> has it (Proposed; the user's play of 2026-11-12, a Rook sent home beside an
    /// enemy Raja whose seat plays after its own): an enemy whose turn comes first may start any exchange; but against
    /// one whose turn comes later, the seat moves first, and saves the piece it would lose most (by moving it, or by
    /// taking its attacker), so only its second-worst counts.
    /// </summary>
    private static int LossOfSeat(
        DecisionRequest request,
        GameState after,
        ImmutableDictionary<SeatId, int> turns,
        IGrouping<SeatId, KeyValuePair<CellId, Piece>> pieces,
        ILookup<CellId, Attack> threats,
        ILookup<CellId, Attack> guards
    )
    {
        int turn = turns.GetValueOrDefault(pieces.Key, int.MaxValue);
        ImmutableList<(int First, int All)> losses =
        [
            .. pieces.Select(occupant =>
            {
                ImmutableList<Attack> all = [.. threats[occupant.Key]];
                int worth = request.Rules.KeepingWorthOf(occupant.Value.Kind);
                ImmutableList<int> retakers = WorthsOf(request, guards[occupant.Key]);
                bool isOnThrone = IsThroneOf(request, after, occupant.Key);
                ImmutableList<Attack> first =
                    [.. all.Where(attack => turns.GetValueOrDefault(attack.Attacker.Owner) < turn)];
                return (
                    LossOf(request, worth, first, retakers, isOnThrone),
                    LossOf(request, worth, all, retakers, isOnThrone)
                );
            }),
        ];
        return losses.Select(loss => loss.First)
            .Concat(losses.Select(loss => loss.All).OrderDescending().Skip(1))
            .Max();
    }

    /// <summary>
    /// When each seat moves next, from the state's: first, the seat whose turn it is (or whose decision the game
    /// waits on), unless it is of the bot's team, which has had its turn, and so moves last; then each after it, in
    /// the order they sit (Proposed: the turn goes round the seats in their order, as every ruleset so far has it).
    /// </summary>
    private static ImmutableDictionary<SeatId, int> TurnsOf(DecisionRequest request, GameState state)
    {
        SeatId? now = state.ActingFor ?? state.DecidingSeat;
        int count = state.Seats.Count;
        int at = state.Seats.FindIndex(seat => seat.Id == now);
        return state.Seats
            .Select((seat, index) => (seat.Id, Turn: seat.Id == now && state.Teams.AreAllied(seat.Id, request.Seat)
                ? count
                : (index - at + count) % count
            ))
            .ToImmutableDictionary(seat => seat.Id, seat => seat.Turn);
    }

    /// <summary>
    /// What an enemy could win by starting an exchange for a piece worth keeping <paramref name="target"/>; on the
    /// bot's throne, an enemy leader taking it first also wins the throne, worth as much as that leader (as
    /// an empty throne's threat counts it).
    /// </summary>
    private static int LossOf(
        DecisionRequest request,
        int target,
        ImmutableList<Attack> threats,
        ImmutableList<int> retakers,
        bool isOnThrone
    )
    {
        ImmutableList<int> takers = WorthsOf(request, threats);
        IEnumerable<int> entries = isOnThrone
            ? threats
                .Where(attack => request.Rules.IsLeader(attack.Attacker.Kind))
                .Select(attack => request.Rules.KeepingWorthOf(attack.Attacker.Kind))
                .Select(leader => Math.Max(0, target + leader - Exchange(leader, retakers, takers.Remove(leader))))
            : [];
        return entries.Append(Exchange(target, takers, retakers)).Max();
    }

    private static bool IsThroneOf(DecisionRequest request, GameState after, CellId cell) =>
        request.Rules.ThronesOf(after).Contains(new KeyValuePair<SeatId, CellId>(request.Seat, cell));

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
        request.Rules.ThronesOf(after).TryGetValue(request.Seat, out CellId throne)
        && after.Position.PieceAt(throne) is null
            ? threats[throne]
                .Where(attack => request.Rules.IsLeader(attack.Attacker.Kind))
                .Select(attack => request.Rules.WorthOf(attack.Attacker.Kind))
                .Append(0)
                .Max()
            : 0;

    /// <summary>The attacks of the bot's team (<paramref name="isAllied"/>) or of its enemies, by target.</summary>
    internal static ILookup<CellId, Attack> AttacksBy(DecisionRequest request, GameState after, bool isAllied) =>
        after.Seats
            .Where(seat => after.Teams.AreAllied(seat.Id, request.Seat) == isAllied)
            .SelectMany(seat => request.Rules.AttacksOf(after, seat.Id))
            .ToLookup(attack => attack.Target);

    /// <summary>
    /// What the attackers are worth keeping (<see cref="IRulesApplying.KeepingWorthOf"/>), as each may be lost
    /// taking: the cheapest first.
    /// </summary>
    internal static ImmutableList<int> WorthsOf(DecisionRequest request, IEnumerable<Attack> attacks) =>
        [.. attacks.Select(attack => request.Rules.KeepingWorthOf(attack.Attacker.Kind)).Order()];
}
