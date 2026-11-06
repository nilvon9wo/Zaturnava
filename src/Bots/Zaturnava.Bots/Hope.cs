using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// What the bot's team could hope to take back within <see cref="Rounds"/> rounds (rules.md §10, Stated
/// 2026-11-06): for each of its pieces, the most it could win - the most valuable enemy piece it could reach, or,
/// for a leader, an unguarded enemy throne, worth that seat's pieces it would come to command - discounted by how
/// far that is: worth × (1 − moves needed ÷ moves it may expect in those rounds, as the dice fall), nothing if
/// farther. The pieces in its way are as they stand; a piece reached is not counted twice only by chance.
/// </summary>
internal static class Hope
{
    /// <summary>How many rounds ahead the team looks.</summary>
    public const int Rounds = 10;

    public static double Of(DecisionRequest request)
    {
        GameState state = request.State;
        MoveGenerator moves = new(
            request.Rules.Topology,
            state.Position.Occupants.Values
                .Select(piece => piece.Kind)
                .Distinct()
                .ToImmutableDictionary(kind => kind, kind => request.Rules.GuideOf(kind).Kind)
        );
        ImmutableDictionary<CellId, int> thrones = Thrones(request);
        return state.Position.Occupants
            .Where(occupant => IsOurs(request, occupant.Value))
            .Sum(occupant => Best(request, moves, thrones, occupant.Key, occupant.Value));
    }

    /// <summary>
    /// The enemy thrones no enemy piece guards, each with what the pieces of its seat on the board are worth.
    /// </summary>
    private static ImmutableDictionary<CellId, int> Thrones(DecisionRequest request)
    {
        GameState state = request.State;
        ImmutableHashSet<CellId> guarded =
        [
            .. state.Seats
                .Where(seat => !state.Teams.AreAllied(seat.Id, request.Seat))
                .SelectMany(seat => request.Rules.AttacksOf(state, seat.Id))
                .Select(attack => attack.Target),
        ];
        return request.Rules.ThronesOf(state)
            .Where(throne => !state.Teams.AreAllied(throne.Key, request.Seat) && !guarded.Contains(throne.Value))
            .ToImmutableDictionary(
                throne => throne.Value,
                throne => state.Position.Occupants.Values
                    .Where(piece => piece.Owner == throne.Key)
                    .Sum(piece => request.Rules.WorthOf(piece.Kind))
            );
    }

    /// <summary>
    /// The most the piece on <paramref name="from"/> could win, discounted: searched move by move, as far as it may
    /// expect to move in <see cref="Rounds"/> rounds; nothing, for a piece that does not move.
    /// </summary>
    private static double Best(
        DecisionRequest request,
        MoveGenerator moves,
        ImmutableDictionary<CellId, int> thrones,
        CellId from,
        Piece piece
    ) =>
        request.Rules.GuideOf(piece.Kind).Kind.Movement.IsEmpty
            ? 0
            : Searched(request, moves, thrones, from, piece);

    private static double Searched(
        DecisionRequest request,
        MoveGenerator moves,
        ImmutableDictionary<CellId, int> thrones,
        CellId from,
        Piece piece
    )
    {
        GameState state = request.State;
        double expected = Rounds * request.Rules.MovesPerTurn(piece.Kind);
        Seat seat = state.SeatOf(piece.Owner);
        Position without = new(state.Position.Occupants.Remove(from));
        bool isLeader = request.Rules.IsLeader(piece.Kind);
        double best = 0;
        ImmutableHashSet<CellId> seen = [from];
        ImmutableList<CellId> frontier = [from];
        for (int needed = 1; needed < expected && !frontier.IsEmpty; needed++)
        {
            double discount = 1 - (needed / expected);
            ImmutableList<Move> reached =
            [
                .. frontier.SelectMany(cell =>
                    moves.MovesFrom(without.Place(cell, piece), cell, seat, target => IsTheirs(request, target))
                ),
            ];
            best = reached
                .Select(move => discount * Worth(request, thrones, isLeader, move))
                .Append(best)
                .Max();
            ImmutableHashSet<CellId> known = seen;
            ImmutableList<CellId> next =
            [
                .. reached
                    .Where(move => move.Captured is null && !known.Contains(move.To))
                    .Select(move => move.To)
                    .Distinct(),
            ];
            seen = seen.Union(next);
            frontier = next;
        }

        return best;
    }

    /// <summary>
    /// What a move would win: the piece it takes; for a leader, an unguarded enemy throne reached, if worth more.
    /// </summary>
    private static int Worth(
        DecisionRequest request,
        ImmutableDictionary<CellId, int> thrones,
        bool isLeader,
        Move move
    )
    {
        int taken = move.Captured is { } captured ? request.Rules.WorthOf(captured.Kind) : 0;
        int throne = isLeader ? thrones.GetValueOrDefault(move.To) : 0;
        return Math.Max(taken, throne);
    }

    private static bool IsOurs(DecisionRequest request, Piece piece) =>
        request.State.Teams.AreAllied(piece.Owner, request.Seat) && !request.State.Retired.Contains(piece.Owner);

    private static bool IsTheirs(DecisionRequest request, Piece piece) =>
        !request.State.Teams.AreAllied(piece.Owner, request.Seat) && !request.State.Retired.Contains(piece.Owner);
}
