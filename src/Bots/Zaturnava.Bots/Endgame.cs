using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// The endgame, as the stronger bots play it (the user's request of 2026-11-25: "a search to the end of the game when
/// few pieces are left, and learned patterns for common endings such as two Rooks against a lone Raja"). With few
/// pieces left (<see cref="IsEndgame"/>), a position is cheap to look into, so they look much further ahead
/// (<see cref="Budget"/>); and they press the enemy's leaders as a ladder mate does (<see cref="Pressure"/>): each
/// cell a leader could flee to that the team attacks, or that the board's edge or its own pieces take from it, is a
/// cell closer to taking it, and a leader attacked is closer still. So two Rooks against a lone Raja close in on it,
/// rather than sail to nowhere. Proposed, all of it, for playing to tune.
/// </summary>
internal static class Endgame
{
    /// <summary>
    /// How many of a board's cells make room for each piece of an endgame (Proposed): on a board of 64 cells, as
    /// Chaturaji 2027's, eight pieces of every seat still playing, or fewer, make an endgame.
    /// </summary>
    public const int CellsPerPiece = 8;

    /// <summary>How much further the bots look in an endgame than before it, as positions they may judge.</summary>
    public const int Budget = 16_000;

    /// <summary>How long Sage, which otherwise looks no further than its own turn, may think in an endgame.</summary>
    public static readonly TimeSpan SageLimit = TimeSpan.FromSeconds(5);

    /// <summary>How many actions Sage follows at each decision past its first, in an endgame.</summary>
    public const int Width = 6;

    /// <summary>What a cell a leader could flee to weighs, by the weight of a point of material (Proposed).</summary>
    public const double PerEscape = 0.25;

    /// <summary>What a leader attacked weighs besides, by the weight of a point of material (Proposed).</summary>
    public const double PerAttack = 0.5;

    /// <summary>
    /// Whether few enough pieces are left on the board, for its size (<see cref="CellsPerPiece"/>), those of seats
    /// retired aside.
    /// </summary>
    public static bool IsEndgame(IRulesApplying rules, GameState state) =>
        rules.Topology.Cells.Count / CellsPerPiece is > 0 and var most
        && state.Position.Occupants.Values.Count(piece => IsPlaying(state, piece)) <= most;

    /// <summary>
    /// How hard the team presses the enemy's leaders and heavy pieces, each by its share (<see cref="Hunting"/>): for
    /// each on the board, each cell it could move to that the team does not attack counts against, at
    /// <see cref="PerEscape"/>; and the piece attacked counts for, at <see cref="PerAttack"/>; scaled by
    /// <paramref name="scale"/>, the weight of a point of material.
    /// </summary>
    public static double Pressure(
        DecisionRequest request,
        GameState state,
        ILookup<CellId, Attack> ours,
        double scale,
        Hunting hunting
    )
    {
        ImmutableList<(SeatId Seat, CellId Cell, double Share)> pressed =
        [
            .. state.Position.Occupants
                .Where(occupant => !state.Teams.AreAllied(occupant.Value.Owner, request.Seat)
                    && IsPlaying(state, occupant.Value)
                )
                .Select(occupant => (
                    occupant.Value.Owner,
                    occupant.Key,
                    request.Rules.IsLeader(occupant.Value.Kind) ? hunting.Leaders
                        : request.Rules.IsHeavy(occupant.Value.Kind) ? hunting.Heavies
                        : 0
                ))
                .Where(piece => piece.Item3 > 0),
        ];
        ImmutableList<Chaser> chasers = hunting.Timed ? ChasersOf(request, state) : [];
        ImmutableDictionary<SeatId, int> turns = hunting.Timed
            ? Careful.ArmyTurnsOf(request, state)
            : [];
        double pressure = hunting.Escapes is { } most
            ? pressed.Sum(piece => piece.Share
                * Cornered(request, state, piece.Seat, piece.Cell, ours, most, [
                    .. chasers.Where(chaser => chaser.Turn < turns.GetValueOrDefault(piece.Seat, int.MaxValue)),
                ])
            )
            : pressed
                .GroupBy(piece => (piece.Seat, piece.Share))
                .Sum(same => same.Key.Share
                    * Pressed(request, state, same.Key.Seat, [.. same.Select(piece => piece.Cell)], ours)
                );
        return scale * pressure;
    }

    /// <summary>
    /// How cornered one enemy piece is (the user, 2026-11-27: "it only just needs one unguarded square ... or whether
    /// there will be time to escape"): attacked, at <see cref="PerAttack"/>; and each cell it could flee to that the
    /// team does not attack, but no more than <paramref name="most"/> of them, at <see cref="PerEscape"/>, as likely
    /// to count as the dice are to let it move at all (<see cref="IRulesApplying.MovesPerTurn"/>). Each cell counts as
    /// safe as the <paramref name="chasers"/>, the team's pieces that move before the piece does, leave it: a chaser
    /// that could cover it in one move takes from its safety as likely as the dice are to let that chaser move (the
    /// user, 2026-11-27: a cell several steps from a piece "could be as little as one turn away if both the enemy and
    /// the ally are able to move it twice per turn ... the (im)probability should be weighed towards the safety of the
    /// square"); with none, each counts whole.
    /// </summary>
    private static double Cornered(
        DecisionRequest request,
        GameState state,
        SeatId seat,
        CellId cell,
        ILookup<CellId, Attack> ours,
        int most,
        ImmutableList<Chaser> chasers
    )
    {
        double safest = request.Rules.MovesOf(state, seat)
            .Where(move => move.From == cell)
            .Select(move => move.Target)
            .Distinct()
            .Where(target => !ours.Contains(target))
            .Select(target => chasers
                .Where(chaser => chaser.Covers.Contains(target))
                .Aggregate(1d, (safety, chaser) => safety * (1 - chaser.Chance))
            )
            .OrderDescending()
            .Take(most)
            .Sum();
        double chance = Math.Min(1, request.Rules.MovesPerTurn(state.Position.PieceAt(cell)!.Kind));
        return (ours.Contains(cell) ? PerAttack : 0) - (PerEscape * chance * safest);
    }

    /// <summary>
    /// One of the team's pieces as it may chase: when its army moves next (<see cref="Careful.ArmyTurnsOf"/>), how
    /// likely the dice are to let it move, and the cells it would attack after any one move of its.
    /// </summary>
    private sealed record Chaser(int Turn, double Chance, ImmutableHashSet<CellId> Covers);

    /// <summary>The team's pieces, each as it may chase.</summary>
    private static ImmutableList<Chaser> ChasersOf(DecisionRequest request, GameState state)
    {
        ImmutableDictionary<SeatId, int> turns = Careful.ArmyTurnsOf(request, state);
        return
        [
            .. state.Seats
                .Where(army => state.Teams.AreAllied(army.Id, request.Seat) && !state.Retired.Contains(army.Id))
                .SelectMany(army => request.Rules.MovesOf(state, army.Id)
                    .GroupBy(move => move.From)
                    .Select(moves => new Chaser(
                        turns.GetValueOrDefault(army.Id, int.MaxValue),
                        Math.Min(1, request.Rules.MovesPerTurn(moves.First().Attacker.Kind)),
                        [
                            .. moves.Select(move => move.Target).Distinct().SelectMany(target =>
                                CoveredFrom(request, state, army.Id, moves.Key, target)
                            ),
                        ]
                    ))
                ),
        ];
    }

    /// <summary>How hard one enemy's pieces, at <paramref name="cells"/>, are pressed.</summary>
    private static double Pressed(
        DecisionRequest request,
        GameState state,
        SeatId seat,
        ImmutableHashSet<CellId> cells,
        ILookup<CellId, Attack> ours
    )
    {
        ImmutableList<Attack> moves = [.. request.Rules.MovesOf(state, seat).Where(move => cells.Contains(move.From))];
        int free = moves.Select(move => move.Target).Distinct().Count(target => !ours.Contains(target));
        int attacked = cells.Count(ours.Contains);
        return (PerAttack * attacked) - (PerEscape * free);
    }

    /// <summary>What the army's piece on <paramref name="from"/> attacks, moved to <paramref name="to"/>.</summary>
    private static IEnumerable<CellId> CoveredFrom(
        DecisionRequest request,
        GameState state,
        SeatId army,
        CellId from,
        CellId to
    ) =>
        request.Rules.AttacksOf(state with { Position = state.Position.Relocate(from, to) }, army)
            .Where(attack => attack.From == to)
            .Select(attack => attack.Target);

    /// <summary>
    /// The weight of a point of material, by <paramref name="weights"/>: none, if they weigh none, so that weights
    /// that leave material out leave the pressure out too.
    /// </summary>
    public static double ScaleOf(SageWeights weights) => Math.Abs(weights.Material.Mean);

    /// <summary>A piece on the board that is not retired.</summary>
    private static bool IsPlaying(GameState state, Piece piece) => !state.Retired.Contains(piece.Owner);
}
