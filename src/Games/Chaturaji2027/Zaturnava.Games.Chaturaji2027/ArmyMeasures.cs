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
/// <item>and how many of those go back towards its own edge: pieces that can retreat, when they must.</item>
/// </list>
/// </summary>
internal sealed class ArmyMeasures(
    ICellNavigating grid,
    ImmutableHashSet<PieceKindId> pawns,
    Func<GameState, CellId, Piece, IEnumerable<Move>> movesFrom
)
{
    public const string PawnProgress = "pawn-progress";
    public const string NearPromotion = "pawns-near-promotion";
    public const string Reach = "reach";
    public const string Retreat = "retreat";

    private readonly ICellNavigating _grid = grid;
    private readonly ImmutableHashSet<PieceKindId> _pawns = pawns;
    private readonly Func<GameState, CellId, Piece, IEnumerable<Move>> _movesFrom = movesFrom;

    public static ImmutableList<string> Names { get; } = [PawnProgress, NearPromotion, Reach, Retreat];

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
        ];
    }

    /// <summary>How many steps there are from the cell to the board's edge, that way.</summary>
    private int StepsFrom(CellId cell, Direction direction) =>
        this._grid.Step(cell, direction) is { } next
            ? 1 + this.StepsFrom(next, direction)
            : 0;
}
