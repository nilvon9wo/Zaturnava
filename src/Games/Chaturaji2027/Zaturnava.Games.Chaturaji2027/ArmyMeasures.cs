using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// What a bot may weigh of an army beyond what every ruleset has: the parts a piece's worth is made of, as they
/// stand on the board (Proposed), not the worth each kind is given. In order (<see cref="Names"/>), as a network
/// that learned before the later ones were offered sees only those it knew:
/// <list type="bullet">
/// <item>how far its Pawns have come towards the far edge, where a Pawn may be promoted (rules.md §8): for each,
/// the rows it has moved forward from the row one in from its own army's edge, where Pawns start;</item>
/// <item>how near they are to promoting: for each, one over the rows it has still to go, so that a Pawn a row
/// away counts 1, two rows away a half, and one just starting out little;</item>
/// <item>its pieces' reach where they stand: how many moves they have, all told, a piece by the edge or hemmed
/// in having fewer, and a quick one more;</item>
/// <item>how many of those go back towards its own edge: pieces that can retreat, when they must;</item>
/// <item>how many of its leaders stand where an enemy of its could take them next;</item>
/// <item>how many of the thrones it commands by are in danger (rules.md §6): its own, should an enemy leader
/// be able to step onto it, or take its leader sitting there, and each other throne its leader sits on, should an
/// enemy be able to take that leader;</item>
/// <item>how many pieces of other armies it commands on its own turns (§6.1): pieces that may move on more
/// turns of a round than their own army's, so that one may take and then get away;</item>
/// <item>and what its pieces are worth that stand where an enemy's prisoner would land, should it escape (§7):
/// its starting square, a throne for a leader, deep in the enemy's kingdom.</item>
/// </list>
/// </summary>
internal sealed class ArmyMeasures(
    ICellNavigating grid,
    ImmutableHashSet<PieceKindId> pawns,
    Func<GameState, CellId, Piece, IEnumerable<Move>> movesFrom,
    Func<GameState, SeatId, IEnumerable<Attack>> attacksOf,
    ThroneControl control,
    Func<GameState, PieceId, CellId> homeOf,
    Func<PieceKindId, int> worthOf
)
{
    public const string PawnProgress = "pawn-progress";
    public const string NearPromotion = "pawns-near-promotion";
    public const string Reach = "reach";
    public const string Retreat = "retreat";
    public const string LeadersAttacked = "leaders-attacked";
    public const string ThronesInDanger = "thrones-in-danger";
    public const string Commanded = "commanded";
    public const string UnderEscapes = "under-escapes";

    private readonly ICellNavigating _grid = grid;
    private readonly ImmutableHashSet<PieceKindId> _pawns = pawns;
    private readonly Func<GameState, CellId, Piece, IEnumerable<Move>> _movesFrom = movesFrom;
    private readonly Func<GameState, SeatId, IEnumerable<Attack>> _attacksOf = attacksOf;
    private readonly ThroneControl _control = control;
    private readonly Func<GameState, PieceId, CellId> _homeOf = homeOf;
    private readonly Func<PieceKindId, int> _worthOf = worthOf;

    public static ImmutableList<string> Names { get; } =
        [PawnProgress, NearPromotion, Reach, Retreat, LeadersAttacked, ThronesInDanger, Commanded, UnderEscapes];

    public ImmutableList<double> Of(GameState state, SeatId seat)
    {
        Direction forward = state.SeatOf(seat).Forward;
        Direction back = new(-forward.First, -forward.Second);
        ImmutableList<KeyValuePair<CellId, Piece>> army =
            [.. state.Position.Occupants.Where(occupant => occupant.Value.Owner == seat)];
        ImmutableList<CellId> pawnCells =
            [.. army.Where(occupant => this._pawns.Contains(occupant.Value.Kind)).Select(occupant => occupant.Key)];
        ImmutableList<Move> moves =
            [.. army.SelectMany(occupant => this._movesFrom(state, occupant.Key, occupant.Value))];
        return
        [
            pawnCells.Sum(cell => this.StepsFrom(cell, back) - 1),
            pawnCells.Sum(cell => 1d / Math.Max(1, this.StepsFrom(cell, forward))),
            moves.Count,
            moves.Count(move => this.StepsFrom(move.To, back) < this.StepsFrom(move.From, back)),
            .. this.Danger(state, seat, army),
            this._control.CommandedBy(state, seat)
                .Sum(command => state.Position.Occupants.Values.Count(piece => piece.Owner == command.Army)),
            this.UnderEscapesOf(state, seat),
        ];
    }

    /// <summary>
    /// What the seat's pieces are worth that stand where an enemy's prisoner, whoever holds it, would land escaping.
    /// </summary>
    private int UnderEscapesOf(GameState state, SeatId seat)
    {
        ImmutableHashSet<CellId> landings =
        [
            .. state.Prisoners
                .Where(prisoner => !state.Teams.AreAllied(prisoner.Piece.Owner, seat))
                .Select(prisoner => this._homeOf(state, prisoner.Piece.Id)),
        ];
        return state.Position.Occupants
            .Where(occupant => occupant.Value.Owner == seat && landings.Contains(occupant.Key))
            .Sum(occupant => this._worthOf(occupant.Value.Kind));
    }

    /// <summary>
    /// How many of the seat's leaders an enemy could take next, and how many of the thrones it commands by are in
    /// danger.
    /// </summary>
    private ImmutableList<double> Danger(GameState state, SeatId seat, ImmutableList<KeyValuePair<CellId, Piece>> army)
    {
        ImmutableList<Attack> threats =
        [
            .. state.Seats
                .Where(enemy => !state.Teams.AreAllied(enemy.Id, seat))
                .SelectMany(enemy => this._attacksOf(state, enemy.Id)),
        ];
        ImmutableHashSet<CellId> attacked = [.. threats.Select(attack => attack.Target)];
        ImmutableHashSet<CellId> byLeaders =
            [.. threats.Where(attack => this._control.IsLeader(attack.Attacker.Kind)).Select(attack => attack.Target)];
        ImmutableHashSet<CellId> leaders =
            [.. army.Where(occupant => this._control.IsLeader(occupant.Value.Kind)).Select(occupant => occupant.Key)];
        ImmutableHashSet<CellId> thrones =
        [
            this._control.ThronesOf(state)[seat],
            .. leaders.Where(cell => this._control.OwnerOf(state, cell) is not null),
        ];
        return
        [
            leaders.Count(attacked.Contains),
            thrones.Count(throne =>
                byLeaders.Contains(throne) || (leaders.Contains(throne) && attacked.Contains(throne))
            ),
        ];
    }

    /// <summary>How many steps there are from the cell to the board's edge, that way.</summary>
    private int StepsFrom(CellId cell, Direction direction) =>
        this._grid.Step(cell, direction) is { } next
            ? 1 + this.StepsFrom(next, direction)
            : 0;
}
