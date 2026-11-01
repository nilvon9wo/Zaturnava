using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// How far an army's Pawns have come towards the far edge, where a Pawn may be promoted (rules.md §8): for
/// each, the rows it has moved forward from the row one in from its own army's edge, where Pawns start.
/// </summary>
internal sealed class Progress(ICellNavigating grid, ImmutableHashSet<PieceKindId> pawns)
{
    public const string PawnProgress = "pawn-progress";

    private readonly ICellNavigating _grid = grid;
    private readonly ImmutableHashSet<PieceKindId> _pawns = pawns;

    public int Of(GameState state, SeatId seat)
    {
        Direction forward = state.SeatOf(seat).Forward;
        Direction back = new(-forward.First, -forward.Second);
        return state.Position.Occupants
            .Where(occupant => occupant.Value.Owner == seat && this._pawns.Contains(occupant.Value.Kind))
            .Sum(occupant => this.StepsFrom(occupant.Key, back) - 1);
    }

    /// <summary>How many steps there are from the cell to the board's edge, that way.</summary>
    private int StepsFrom(CellId cell, Direction direction) =>
        this._grid.Step(cell, direction) is { } next
            ? 1 + this.StepsFrom(next, direction)
            : 0;
}
