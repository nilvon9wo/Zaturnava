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
/// gains (as <see cref="Greedy"/> counts it), less what its opponents could do to the bot's team
/// once it is done. That is what an enemy could win by starting an exchange on each cell where one of
/// the team's pieces stands, if both sides go on taking while it pays: each enemy army, in the order they move, taking
/// the most it could, and half of its next (<see cref="SecondShare"/>); and, if the bot's throne is
/// empty, what the enemy leader who could step onto it is worth. A piece on the bot's throne does not
/// keep an enemy leader off it, but only makes it take that piece to get there: such a taking wins the
/// piece and the throne both. The dice are left aside, but not whose turn comes next: an army of the team
/// that moves before an enemy, on its own seat's turn or on that of an ally commanding it, saves the piece
/// it would lose most to it (<see cref="LossesOfSeat"/>); but the team's taking back is only as sure as its dice. Half
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
        GameState after = request.Rules.ApplyLegal(request.State, action).State;
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
        ImmutableDictionary<SeatId, int> turns = ArmyTurnsOf(request, after);
        ImmutableList<Loss> losses =
        [
            .. after.Position.Occupants
                .Where(occupant => after.Teams.AreAllied(occupant.Value.Owner, request.Seat))
                .GroupBy(occupant => occupant.Value.Owner)
                .SelectMany(pieces => LossesOfSeat(request, after, turns, pieces, threats, guards))
                .Where(loss => loss.Worth > 0),
        ];
        double total = losses
            .SelectMany(loss => loss.Takers)
            .Distinct()
            .OrderBy(army => turns.GetValueOrDefault(army, int.MaxValue))
            .Aggregate(
                (Total: 0d, Left: losses),
                (sofar, army) =>
                {
                    ImmutableList<Loss> taken =
                    [
                        .. sofar.Left
                            .Where(loss => loss.Takers.Contains(army))
                            .OrderByDescending(loss => loss.Worth)
                            .Take(2),
                    ];
                    return (
                        sofar.Total + taken.Select((loss, at) => at == 0 ? loss.Worth : SecondShare * loss.Worth).Sum(),
                        sofar.Left.RemoveRange(taken)
                    );
                }
            )
            .Total;
        return (int)Math.Round(total, MidpointRounding.AwayFromZero) + ThroneThreat(request, after, threats);
    }

    /// <summary>What an enemy could win from one piece of the team, and the enemy armies that could start it.</summary>
    private sealed record Loss(double Worth, ImmutableHashSet<SeatId> Takers);

    /// <summary>
    /// What an enemy could win from each of the pieces of one seat of the bot's team (<paramref name="pieces"/>), and
    /// which enemy armies could start it, each army's turn coming as <paramref name="turns"/> has it
    /// (<see cref="ArmyTurnsOf"/>; Proposed; the user's play of 2026-11-12, a Rook sent home beside an enemy Raja whose
    /// seat plays after its own): an enemy whose turn comes first may start any exchange; but against one whose turn
    /// comes later, the seat moves first, and saves the piece it would lose most (by moving it, or by taking its
    /// attacker), so that only what an earlier enemy could win from that piece counts.
    /// </summary>
    private static IEnumerable<Loss> LossesOfSeat(
        DecisionRequest request,
        GameState after,
        ImmutableDictionary<SeatId, int> turns,
        IGrouping<SeatId, KeyValuePair<CellId, Piece>> pieces,
        ILookup<CellId, Attack> threats,
        ILookup<CellId, Attack> guards
    )
    {
        int turn = turns.GetValueOrDefault(pieces.Key, int.MaxValue);
        ImmutableList<(Loss First, Loss All)> losses =
        [
            .. pieces.Select(occupant =>
            {
                ImmutableList<Attack> all = [.. threats[occupant.Key]];
                int worth = request.Rules.KeepingWorthOf(occupant.Value.Kind);
                ImmutableList<Retaker> retakers = RetakersOf(request, guards[occupant.Key]);
                bool isOnThrone = IsThroneOf(request, after, occupant.Key);
                ImmutableList<Attack> first =
                    [.. all.Where(attack => turns.GetValueOrDefault(attack.Attacker.Owner) < turn)];
                return (
                    new Loss(LossOf(request, worth, first, retakers, isOnThrone), ArmiesOf(first)),
                    new Loss(LossOf(request, worth, all, retakers, isOnThrone), ArmiesOf(all))
                );
            }),
        ];
        int worstOfAll = losses
            .Select((loss, index) => (loss.All.Worth, Index: index))
            .MaxBy(loss => loss.Worth)
            .Index;
        return losses.Select((loss, index) => index == worstOfAll ? loss.First : loss.All);
    }

    private static ImmutableHashSet<SeatId> ArmiesOf(IEnumerable<Attack> attacks) =>
        [.. attacks.Select(attack => attack.Attacker.Owner)];

    /// <summary>
    /// How much of a second piece an enemy army could take counts besides its first (Proposed, on the user's game of
    /// 2026-11-27: Sage's Rook left a Pawn unguarded while another hung already, and Red's Rook, on a double, took
    /// both): a turn's dice may let it take twice, if they fall right. Each enemy army moving before the team again
    /// takes its own best (the games played of 2026-11-27: with one piece hanging already, a second left to be taken,
    /// or an ally's, cost nothing, though two enemies move in between, and Yellow's Boat and then its Raja went).
    /// </summary>
    private const double SecondShare = 0.5;

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
    /// When each army's pieces move next, from when each seat moves next (<see cref="TurnsOf"/>): at the first turn
    /// of a seat that moves them (<see cref="IRulesApplying.MoversOf"/>), of the army's own side, as the other side's
    /// seats do not move them for it (the designer's case of 2026-11-25: a Yellow Rook, moved on Red's turn by Red's
    /// Raja, taken by Black, who moved before Yellow, but after Red).
    /// </summary>
    internal static ImmutableDictionary<SeatId, int> ArmyTurnsOf(DecisionRequest request, GameState after) =>
        ArmyTurnsFrom(request, after, TurnsOf(request, after));

    private static ImmutableDictionary<SeatId, int> ArmyTurnsFrom(
        DecisionRequest request,
        GameState after,
        ImmutableDictionary<SeatId, int> turns
    ) =>
        after.Seats.ToImmutableDictionary(
            army => army.Id,
            army => request.Rules.MoversOf(after, army.Id)
                .Where(seat => after.Teams.AreAllied(seat, army.Id))
                .Select(seat => turns.GetValueOrDefault(seat, int.MaxValue))
                .Append(turns.GetValueOrDefault(army.Id, int.MaxValue))
                .Min()
        );

    /// <summary>
    /// What an enemy could win by starting an exchange for a piece worth keeping <paramref name="target"/>; on the
    /// bot's throne, an enemy leader taking it first also wins the throne, worth as much as that leader (as
    /// an empty throne's threat counts it). The team's retakes are counted as likely as the dice let each retaker
    /// move (<see cref="Lost"/>).
    /// </summary>
    private static int LossOf(
        DecisionRequest request,
        int target,
        ImmutableList<Attack> threats,
        ImmutableList<Retaker> retakers,
        bool isOnThrone
    )
    {
        ImmutableList<int> takers = WorthsOf(request, threats);
        IEnumerable<double> entries = isOnThrone
            ? threats
                .Where(attack => request.Rules.IsLeader(attack.Attacker.Kind))
                .Select(attack => request.Rules.KeepingWorthOf(attack.Attacker.Kind))
                .Select(leader => Math.Max(0, target + leader - Retaken(leader, retakers, takers.Remove(leader))))
            : [];
        return (int)Math.Round(entries.Append(Lost(target, takers, retakers)).Max(), MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// A piece of the bot's team that could take back on a cell: what it is worth keeping, and how likely it is to be
    /// let move when it must (<see cref="ChanceOf"/>).
    /// </summary>
    private sealed record Retaker(int Worth, double Chance);

    /// <summary>
    /// The team's pieces that could take back, by their <paramref name="attacks"/>: the cheapest first.
    /// </summary>
    private static ImmutableList<Retaker> RetakersOf(DecisionRequest request, IEnumerable<Attack> attacks) =>
    [
        .. attacks
            .Select(attack => new Retaker(
                request.Rules.KeepingWorthOf(attack.Attacker.Kind),
                ChanceOf(request, attack.Attacker.Kind)
            ))
            .OrderBy(retaker => retaker.Worth),
    ];

    /// <summary>
    /// How likely a piece of <paramref name="kind"/> is to be let move in its seat's turn: the moves it may expect
    /// (<see cref="IRulesApplying.MovesPerTurn"/>), but no more than certain (Proposed, on the user's games of
    /// 2026-11-27: Argus sent the user's Rook among Red's Horses, counting on a Pawn to take back, which needed a
    /// Pawn's die).
    /// </summary>
    private static double ChanceOf(DecisionRequest request, PieceKindId kind) =>
        Math.Min(1, request.Rules.MovesPerTurn(kind));

    /// <summary>
    /// What the enemy wins, as <see cref="Exchange"/> counts it, taking <paramref name="target"/> with the cheapest of
    /// its <paramref name="takers"/>, sure to take whenever it pays, while each of the team's retakes comes only as
    /// likely as its retaker's dice.
    /// </summary>
    private static double Lost(int target, ImmutableList<int> takers, ImmutableList<Retaker> retakers) =>
        takers.IsEmpty
            ? 0
            : Math.Max(0, target - Retaken(takers[0], retakers, takers.RemoveAt(0)));

    /// <summary>
    /// What the team wins taking back <paramref name="target"/>, as likely as its cheapest retaker's dice.
    /// </summary>
    private static double Retaken(int target, ImmutableList<Retaker> retakers, ImmutableList<int> takers) =>
        retakers.IsEmpty
            ? 0
            : retakers[0].Chance * Math.Max(0, target - Lost(retakers[0].Worth, takers, retakers.RemoveAt(0)));

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
